using System;
namespace CampusNavigation.Editor
{
    public static class ReviewLoginTests
    {
        public static void Run()
        {
            if(!ReviewLogin.Valid("student","Campus123!"))throw new Exception("Valid review account rejected");
            if(ReviewLogin.Valid("student","wrong"))throw new Exception("Wrong password accepted");
            if(ReviewLogin.Valid("other","Campus123!"))throw new Exception("Unknown user accepted");
            if(ReviewLogin.Valid(null,null)||ReviewLogin.Valid("student",null))throw new Exception("Empty credentials accepted");
        }
    }
}
