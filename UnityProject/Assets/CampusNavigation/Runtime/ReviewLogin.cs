using System;
using System.Security.Cryptography;
using System.Text;
namespace CampusNavigation
{
    // Fixed, local assessment account only. Not production authentication.
    public static class ReviewLogin
    {
        public static bool Valid(string username,string password)
        {
            if((username??"").Trim()!="student"||password==null)return false;
            using(var sha=SHA256.Create())
            {
                string digest=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(password))).Replace("-","").ToLowerInvariant();
                return digest=="f7b48db59f88968349ab4fbb70ecb080b0db8e8354beba96d9cc75b829fd5f65";
            }
        }
    }
}
