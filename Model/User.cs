using Plugin.Firebase.Firestore;

namespace TaskManagement.Model;

public class User
{
    [FirestoreDocumentId]
    public string Id { get; set; }

    [FirestoreProperty("Username")]
    public string Username { get; set; }
    [FirestoreProperty("Email")]
    public string Email { get; set; }
    [FirestoreProperty("Role")]
    public string Role { get; set; }      // "Admin" or "Member"
    [FirestoreProperty("CompanyId")]
    public string CompanyId { get; set; } // Reference to Company
}
