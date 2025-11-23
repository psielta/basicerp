using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EntityFrameworkProject.Models
{
    [Table("stock_outbox_event")]
    public class StockOutboxEvent
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("organization_id")]
        public Guid? OrganizationId { get; set; }

        [Required]
        [Column("aggregate_type")]
        public string AggregateType { get; set; }

        [Column("aggregate_id")]
        public Guid? AggregateId { get; set; }

        [Required]
        [Column("event_type")]
        public string EventType { get; set; }

        [Required]
        [Column("payload")]
        public string Payload { get; set; }

        [Column("topic")]
        public string Topic { get; set; }

        [Column("key")]
        public string Key { get; set; }

        [Column("occurred_at")]
        public DateTime OccurredAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("published_at")]
        public DateTime? PublishedAt { get; set; }

        [Column("status")]
        public short Status { get; set; }

        [Column("error")]
        public string Error { get; set; }

        [Column("trace_id")]
        public string TraceId { get; set; }

        public Organization Organization { get; set; }
    }
}
