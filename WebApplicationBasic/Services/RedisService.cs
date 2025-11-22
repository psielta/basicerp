using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;
using Serilog;

namespace WebApplicationBasic.Services
{
    public static class RedisService
    {
        private static readonly object _lock = new object();
        private static ConnectionMultiplexer _connection;
        private static ConfigurationOptions _options;
        private static string _keyPrefix = string.Empty;

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        static RedisService()
        {
            Initialize();
        }

        private static void Initialize()
        {
            try
            {
                // Lê de AppSettings ou ConnectionStrings
                var raw = ConfigurationManager.AppSettings["Redis"]
                          ?? ConfigurationManager.ConnectionStrings["Redis"]?.ConnectionString
                          ?? "localhost:6379";

                _options = ConfigurationOptions.Parse(raw);
                _options.AbortOnConnectFail = false;
                if (_options.ConnectTimeout == 0) _options.ConnectTimeout = 5000;
                if (_options.SyncTimeout == 0) _options.SyncTimeout = 5000;

                // allowAdmin (necessário para FlushDatabase e Keys/SCAN)
                if (bool.TryParse(ConfigurationManager.AppSettings["RedisAllowAdmin"], out var allowAdmin))
                {
                    _options.AllowAdmin = allowAdmin;
                }

                _keyPrefix = ConfigurationManager.AppSettings["RedisKeyPrefix"] ?? string.Empty;

                ConnectNewMultiplexer();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Falha ao inicializar conexão Redis.");
            }
        }

        private static void ConnectNewMultiplexer()
        {
            var newConn = ConnectionMultiplexer.Connect(_options);

            newConn.ConnectionFailed += (s, e) =>
                Log.Warning(e.Exception, "Redis: conexão falhou ({FailureType}) em {EndPoint}.", e.FailureType, e.EndPoint);

            newConn.ConnectionRestored += (s, e) =>
                Log.Information("Redis: conexão restaurada ({FailureType}) em {EndPoint}.", e.FailureType, e.EndPoint);

            newConn.ConfigurationChanged += (s, e) =>
                Log.Information("Redis: configuração alterada em {EndPoint}.", e.EndPoint);

            newConn.ErrorMessage += (s, e) =>
                Log.Warning("Redis: erro: {Message}", e.Message);

            var old = _connection;
            _connection = newConn;

            try { old?.Dispose(); } catch { /* ignore */ }
        }

        private static ConnectionMultiplexer Connection
        {
            get
            {
                if (_connection == null || !_connection.IsConnected)
                {
                    lock (_lock)
                    {
                        if (_connection == null || !_connection.IsConnected)
                        {
                            ConnectNewMultiplexer();
                        }
                    }
                }
                return _connection;
            }
        }

        private static IDatabase Database => Connection.GetDatabase();

        private static string ApplyPrefix(string key) =>
            string.IsNullOrEmpty(_keyPrefix) ? key : _keyPrefix + key;

        private static string RemovePrefix(string key)
        {
            if (string.IsNullOrEmpty(_keyPrefix)) return key;
            return key.StartsWith(_keyPrefix, StringComparison.Ordinal) ? key.Substring(_keyPrefix.Length) : key;
        }

        // ------------------ Conexão ------------------

        public static bool IsConnected()
        {
            try { return Connection.IsConnected; }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao verificar conexão Redis");
                return false;
            }
        }

        // ------------------ String ------------------

        public static bool Set(string key, string value)
        {
            try
            {
                return Database.StringSet(ApplyPrefix(key), value);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao salvar string no Redis. Key: {Key}", key);
                return false;
            }
        }

        public static bool Set(string key, string value, TimeSpan expiry)
        {
            try
            {
                var fullKey = ApplyPrefix(key);
                return SetWithExpiryViaSET(fullKey, value, expiry);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao salvar string no Redis com expiração. Key: {Key}, Expiry: {Expiry}", key, expiry);
                return false;
            }
        }

        public static bool Set(string key, string value, TimeSpan? expiry)
        {
            try
            {
                var fullKey = ApplyPrefix(key);
                if (expiry.HasValue)
                    return SetWithExpiryViaSET(fullKey, value, expiry.Value);

                return Database.StringSet(fullKey, value);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao salvar string no Redis. Key: {Key}", key);
                return false;
            }
        }

        public static async Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null)
        {
            try
            {
                var fullKey = ApplyPrefix(key);
                if (expiry.HasValue)
                    return await SetWithExpiryViaSETAsync(fullKey, value, expiry.Value).ConfigureAwait(false);

                return await Database.StringSetAsync(fullKey, value).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao salvar string no Redis (async). Key: {Key}", key);
                return false;
            }
        }

        public static string Get(string key)
        {
            try
            {
                var rv = Database.StringGet(ApplyPrefix(key));
                return rv.HasValue ? (string)rv : null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao obter string do Redis. Key: {Key}", key);
                return null;
            }
        }

        public static async Task<string> GetAsync(string key)
        {
            try
            {
                var rv = await Database.StringGetAsync(ApplyPrefix(key)).ConfigureAwait(false);
                return rv.HasValue ? (string)rv : null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao obter string do Redis (async). Key: {Key}", key);
                return null;
            }
        }

        // ------------------ Objetos (JSON) ------------------

        public static bool Set<T>(string key, T value)
        {
            try
            {
                string json = JsonConvert.SerializeObject(value, JsonSettings);
                return Database.StringSet(ApplyPrefix(key), json);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao salvar objeto no Redis. Key: {Key}, Type: {Type}", key, typeof(T).Name);
                return false;
            }
        }

        public static bool Set<T>(string key, T value, TimeSpan expiry)
        {
            try
            {
                string json = JsonConvert.SerializeObject(value, JsonSettings);
                var fullKey = ApplyPrefix(key);
                return SetWithExpiryViaSET(fullKey, json, expiry);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao salvar objeto no Redis com expiração. Key: {Key}, Type: {Type}, Expiry: {Expiry}", key, typeof(T).Name, expiry);
                return false;
            }
        }

        public static bool Set<T>(string key, T value, TimeSpan? expiry)
        {
            try
            {
                string json = JsonConvert.SerializeObject(value, JsonSettings);
                var fullKey = ApplyPrefix(key);

                if (expiry.HasValue)
                    return SetWithExpiryViaSET(fullKey, json, expiry.Value);

                return Database.StringSet(fullKey, json);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao salvar objeto no Redis. Key: {Key}, Type: {Type}", key, typeof(T).Name);
                return false;
            }
        }

        public static async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            try
            {
                string json = JsonConvert.SerializeObject(value, JsonSettings);
                var fullKey = ApplyPrefix(key);

                if (expiry.HasValue)
                    return await SetWithExpiryViaSETAsync(fullKey, json, expiry.Value).ConfigureAwait(false);

                return await Database.StringSetAsync(fullKey, json).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao salvar objeto no Redis (async). Key: {Key}, Type: {Type}", key, typeof(T).Name);
                return false;
            }
        }

        public static T Get<T>(string key)
        {
            try
            {
                var json = Database.StringGet(ApplyPrefix(key));
                if (!json.HasValue) return default(T);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao obter objeto do Redis. Key: {Key}, Type: {Type}", key, typeof(T).Name);
                return default(T);
            }
        }

        public static async Task<T> GetAsync<T>(string key)
        {
            try
            {
                var json = await Database.StringGetAsync(ApplyPrefix(key)).ConfigureAwait(false);
                if (!json.HasValue) return default(T);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao obter objeto do Redis (async). Key: {Key}, Type: {Type}", key, typeof(T).Name);
                return default(T);
            }
        }

        // ------------------ Metadados de chave ------------------

        public static bool Exists(string key)
        {
            try { return Database.KeyExists(ApplyPrefix(key)); }
            catch (Exception ex) { Log.Error(ex, "Erro ao verificar existência de chave no Redis. Key: {Key}", key); return false; }
        }

        public static bool Remove(string key)
        {
            try { return Database.KeyDelete(ApplyPrefix(key)); }
            catch (Exception ex) { Log.Error(ex, "Erro ao remover chave do Redis. Key: {Key}", key); return false; }
        }

        public static long Remove(params string[] keys)
        {
            try
            {
                RedisKey[] redisKeys = keys.Select(k => (RedisKey)ApplyPrefix(k)).ToArray();
                return Database.KeyDelete(redisKeys);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao remover múltiplas chaves do Redis. Keys: {Keys}", string.Join(", ", keys));
                return 0;
            }
        }

        public static bool SetExpiry(string key, TimeSpan expiry)
        {
            try { return Database.KeyExpire(ApplyPrefix(key), expiry); }
            catch (Exception ex) { Log.Error(ex, "Erro ao definir expiração no Redis. Key: {Key}, Expiry: {Expiry}", key, expiry); return false; }
        }

        public static bool SetExpiry(string key, DateTime absoluteUtc)
        {
            try { return Database.KeyExpire(ApplyPrefix(key), absoluteUtc); }
            catch (Exception ex) { Log.Error(ex, "Erro ao definir expiração absoluta no Redis. Key: {Key}, Date: {Date}", key, absoluteUtc); return false; }
        }

        public static TimeSpan? GetTimeToLive(string key)
        {
            try { return Database.KeyTimeToLive(ApplyPrefix(key)); }
            catch (Exception ex) { Log.Error(ex, "Erro ao obter TTL do Redis. Key: {Key}", key); return null; }
        }

        // ------------------ Admin / varredura ------------------

        public static void FlushDatabase()
        {
            try
            {
                if (!_options.AllowAdmin)
                {
                    Log.Warning("FlushDatabase ignorado: AllowAdmin=false. Habilite 'allowAdmin=true' na connection string (com cuidado).");
                    return;
                }

                foreach (var endpoint in Connection.GetEndPoints())
                {
                    var server = Connection.GetServer(endpoint);
                    if (!server.IsConnected) continue;
                    server.FlushDatabase(Database.Database);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao limpar banco de dados Redis");
            }
        }

        /// <summary>
        /// Busca chaves por padrão (usa SCAN). Evite em produção com muitos dados.
        /// Padrão aceita curingas (ex.: "contdoc:*").
        /// </summary>
        public static IEnumerable<string> SearchKeys(string pattern)
        {
            try
            {
                var results = new HashSet<string>(StringComparer.Ordinal);
                var patternWithPrefix = ApplyPrefix(pattern);

                foreach (var endpoint in Connection.GetEndPoints())
                {
                    var server = Connection.GetServer(endpoint);
                    if (!server.IsConnected) continue;

                    foreach (var key in server.Keys(
                                 database: Database.Database,
                                 pattern: patternWithPrefix,
                                 pageSize: 1000))
                    {
                        results.Add(RemovePrefix(key.ToString()));
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao buscar chaves no Redis. Pattern: {Pattern}", pattern);
                return Enumerable.Empty<string>();
            }
        }

        // ------------------ Contadores ------------------

        public static long Increment(string key, long value = 1)
        {
            try { return Database.StringIncrement(ApplyPrefix(key), value); }
            catch (Exception ex) { Log.Error(ex, "Erro ao incrementar valor no Redis. Key: {Key}, Value: {Value}", key, value); return 0; }
        }

        public static long Decrement(string key, long value = 1)
        {
            try { return Database.StringDecrement(ApplyPrefix(key), value); }
            catch (Exception ex) { Log.Error(ex, "Erro ao decrementar valor no Redis. Key: {Key}, Value: {Value}", key, value); return 0; }
        }

        // ------------------ Helpers ------------------

        /// <summary>
        /// Padrão cache-aside: tenta pegar do cache, se não existir produz, grava e retorna.
        /// </summary>
        public static T GetOrAdd<T>(string key, Func<T> factory, TimeSpan? expiry = null)
        {
            var cached = Get<T>(key);
            if (!EqualityComparer<T>.Default.Equals(cached, default(T)))
                return cached;

            var value = factory();
            Set(key, value, expiry);
            return value;
        }

        public static async Task<T> GetOrAddAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
        {
            var cached = await GetAsync<T>(key).ConfigureAwait(false);
            if (!EqualityComparer<T>.Default.Equals(cached, default(T)))
                return cached;

            var value = await factory().ConfigureAwait(false);
            await SetAsync(key, value, expiry).ConfigureAwait(false);
            return value;
        }

        /// <summary>Fecha a conexão (por exemplo, em Application_End).</summary>
        public static void Shutdown()
        {
            try
            {
                var c = _connection;
                _connection = null;
                c?.Close();
                c?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro ao encerrar conexão Redis");
            }
        }

        private static bool SetWithExpiryViaSET(string fullKey, string value, TimeSpan expiry)
        {
            try
            {
                if (expiry <= TimeSpan.Zero)
                    return Database.StringSet(fullKey, value);

                // Usa SET com PX (ms) para ser agnóstico de versão do cliente
                var args = new object[] { (RedisKey)fullKey, (RedisValue)value, "PX", (long)expiry.TotalMilliseconds };
                var rr = Database.Execute("SET", args);
                var s = rr.ToString();
                return string.Equals(s, "OK", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao executar SET PX no Redis. Key: {Key}", fullKey);
                return false;
            }
        }

        private static async Task<bool> SetWithExpiryViaSETAsync(string fullKey, string value, TimeSpan expiry)
        {
            try
            {
                if (expiry <= TimeSpan.Zero)
                    return await Database.StringSetAsync(fullKey, value).ConfigureAwait(false);

                var args = new object[] { (RedisKey)fullKey, (RedisValue)value, "PX", (long)expiry.TotalMilliseconds };
                var rr = await Database.ExecuteAsync("SET", args).ConfigureAwait(false);
                var s = rr.ToString();
                return string.Equals(s, "OK", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao executar SET PX (async) no Redis. Key: {Key}", fullKey);
                return false;
            }
        }

    }
}
