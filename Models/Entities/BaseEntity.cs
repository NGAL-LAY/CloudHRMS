using CloudHRMS.Utiliteis;
using System.ComponentModel.DataAnnotations;

namespace CloudHRMS.Models.Entities
{
    public abstract class BaseEntity
    {
        [Key]
        public string Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string IpAddress { get; set; } = NetworkHelper.GetLocalIPAddress();
        public bool IsInActive { get; set; }
    }
}
