using System.ComponentModel.DataAnnotations.Schema;
using webBackend.Models;


namespace webBackend.Models;
public class UserAddress
{
    public int Id { get; set; }

    // AspNetUsers tablosundaki Id ile eşleşecek alan
    // Identity varsayılan olarak string (Guid) kullanır.
    public int UserId { get; set; } 

    // BU KISIM EKSİK: İlişkiyi temsil eden nesne
    // "ApplicationUser" yerine projenizdeki Identity sınıfının adını yazın (örn: IdentityUser)
    [ForeignKey("UserId")]
    public virtual AppUser User { get; set; } 

    public string AddressTitle { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Phone { get; set; }
    public string City { get; set; }
    public string District { get; set; }

    public string AddressDetail {get; set;}
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
}