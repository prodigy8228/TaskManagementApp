using Newtonsoft.Json;
using System.Text;

namespace TaskManagement
{
    internal class AuthService
    {

        public async Task<string> GetIdTokenAsync(string email, string password)
        {
            var apiKey = "AIzaSyAJkcrWu6U49gLeXUSeN3KAY-Jt2uJq_6E"; // from Firebase project settings
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";

            var body = new
            {
                email = email,
                password = password,
                returnSecureToken = true
            };

            var json = JsonConvert.SerializeObject(body);
            var response = await new HttpClient().PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();

            var result = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
            return result.idToken; // <-- use this in FirestoreService
        }

    }
}
