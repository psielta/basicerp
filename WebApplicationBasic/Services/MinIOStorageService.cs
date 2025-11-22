using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Serilog;

namespace WebApplicationBasic.Services
{
    public class MinIOStorageService : IStorageService
    {
        private IMinioClient _minioClient;
        private readonly string _bucketName;
        private readonly string _publicUrl;
        private readonly string _endpoint;
        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly bool _useSSL;
        private readonly object _lockObject = new object();

        public MinIOStorageService()
        {
            _endpoint = ConfigurationManager.AppSettings["MinIO:Endpoint"];
            _accessKey = ConfigurationManager.AppSettings["MinIO:AccessKey"];
            _secretKey = ConfigurationManager.AppSettings["MinIO:SecretKey"];
            _useSSL = bool.Parse(ConfigurationManager.AppSettings["MinIO:UseSSL"] ?? "false");

            _bucketName = ConfigurationManager.AppSettings["MinIO:BucketName"];
            _publicUrl = ConfigurationManager.AppSettings["MinIO:PublicUrl"];
        }

        private IMinioClient GetMinioClient()
        {
            if (_minioClient == null)
            {
                lock (_lockObject)
                {
                    if (_minioClient == null)
                    {
                        _minioClient = new MinioClient()
                            .WithEndpoint(_endpoint)
                            .WithCredentials(_accessKey, _secretKey)
                            .WithSSL(_useSSL)
                            .Build();
                    }
                }
            }
            return _minioClient;
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            try
            {
                Log.Information("STORAGE_UPLOAD_START: Iniciando upload do arquivo {FileName} ({ContentType}, {Size} bytes) para bucket {BucketName}",
                    fileName, contentType, fileStream.Length, _bucketName);

                // Garantir que o bucket existe
                await EnsureBucketExistsAsync();

                // Upload do arquivo
                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName)
                    .WithStreamData(fileStream)
                    .WithObjectSize(fileStream.Length)
                    .WithContentType(contentType);

                await GetMinioClient().PutObjectAsync(putObjectArgs);

                var url = GetFileUrl(fileName);

                Log.Information("STORAGE_UPLOAD_SUCCESS: Arquivo {FileName} enviado com sucesso para {Url}",
                    fileName, url);

                // Retornar URL pública
                return url;
            }
            catch (MinioException ex)
            {
                Log.Error(ex, "STORAGE_UPLOAD_FAILED: Falha ao enviar arquivo {FileName} para bucket {BucketName}",
                    fileName, _bucketName);
                throw new Exception($"Erro ao fazer upload do arquivo: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteFileAsync(string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    Log.Warning("STORAGE_DELETE_FAILED: Nome de arquivo vazio fornecido");
                    return false;
                }

                var originalFileName = fileName;

                // Extrair apenas o nome do arquivo da URL se necessário
                if (fileName.StartsWith("http"))
                {
                    var uri = new Uri(fileName);
                    fileName = uri.LocalPath.TrimStart('/');

                    // Remover o bucket name do path se presente
                    if (fileName.StartsWith(_bucketName + "/"))
                    {
                        fileName = fileName.Substring(_bucketName.Length + 1);
                    }
                }

                Log.Information("STORAGE_DELETE_START: Deletando arquivo {FileName} do bucket {BucketName}",
                    fileName, _bucketName);

                var removeObjectArgs = new RemoveObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName);

                await GetMinioClient().RemoveObjectAsync(removeObjectArgs);

                Log.Information("STORAGE_DELETE_SUCCESS: Arquivo {FileName} deletado com sucesso", fileName);
                return true;
            }
            catch (MinioException ex)
            {
                Log.Warning(ex, "STORAGE_DELETE_FAILED: Falha ao deletar arquivo {FileName} do bucket {BucketName}",
                    fileName, _bucketName);
                // Arquivo não existe ou erro ao deletar
                return false;
            }
        }

        public string GetFileUrl(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            // Se já for uma URL completa, retornar como está
            if (fileName.StartsWith("http"))
                return fileName;

            // Construir URL pública
            return $"{_publicUrl}/{_bucketName}/{fileName}";
        }

        public async Task<bool> FileExistsAsync(string fileName)
        {
            try
            {
                var statObjectArgs = new StatObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName);

                var stat = await GetMinioClient().StatObjectAsync(statObjectArgs);
                return stat != null;
            }
            catch (ObjectNotFoundException)
            {
                return false;
            }
            catch (MinioException)
            {
                return false;
            }
        }

        public async Task<bool> EnsureBucketExistsAsync()
        {
            try
            {
                var bucketExistsArgs = new BucketExistsArgs()
                    .WithBucket(_bucketName);

                bool found = await GetMinioClient().BucketExistsAsync(bucketExistsArgs);

                if (!found)
                {
                    Log.Information("STORAGE_BUCKET_CREATE: Criando bucket {BucketName}", _bucketName);

                    var makeBucketArgs = new MakeBucketArgs()
                        .WithBucket(_bucketName);

                    await GetMinioClient().MakeBucketAsync(makeBucketArgs);

                    // Configurar política pública para leitura
                    var policy = @"{
                        ""Version"": ""2012-10-17"",
                        ""Statement"": [
                            {
                                ""Effect"": ""Allow"",
                                ""Principal"": {
                                    ""AWS"": [""*""]
                                },
                                ""Action"": [""s3:GetObject""],
                                ""Resource"": [""arn:aws:s3:::" + _bucketName + @"/*""]
                            }
                        ]
                    }";

                    var setPolicyArgs = new SetPolicyArgs()
                        .WithBucket(_bucketName)
                        .WithPolicy(policy);

                    await GetMinioClient().SetPolicyAsync(setPolicyArgs);

                    Log.Information("STORAGE_BUCKET_CREATED: Bucket {BucketName} criado com sucesso e política pública configurada", _bucketName);
                }
                else
                {
                    Log.Debug("STORAGE_BUCKET_EXISTS: Bucket {BucketName} já existe", _bucketName);
                }

                return true;
            }
            catch (MinioException ex)
            {
                Log.Error(ex, "STORAGE_BUCKET_ERROR: Erro ao criar/verificar bucket {BucketName}", _bucketName);
                throw new Exception($"Erro ao criar/verificar bucket: {ex.Message}", ex);
            }
        }
    }
}