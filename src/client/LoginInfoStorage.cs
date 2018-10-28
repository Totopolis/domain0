using System;
using System.IO;
using System.IO.IsolatedStorage;
namespace Domain0.Api.Client
{
    internal class LoginInfoStorage
    {
        private const string FileName = "LoginInfo";
        private readonly IsolatedStorageFile storage;

        public LoginInfoStorage()
        {
            storage = GetIsolatedStorage();
        }

        public void Delete()
        {
            if (storage.FileExists(FileName))
                storage.DeleteFile(FileName);
        }

        public void Save(AccessTokenResponse data)
        {
            using (var f = new IsolatedStorageFileStream(
                FileName, FileMode.Create, FileAccess.Write, storage))
            using (var sw = new StreamWriter(f))
            {
                sw.Write(data.ToJson());
            }
        }

        public AccessTokenResponse Load()
        {
            if (!storage.FileExists(FileName))
                return null;

            using (var f = storage.OpenFile(FileName, FileMode.Open))
            using (var sr = new StreamReader(f))
            {
                return AccessTokenResponse.FromJson(sr.ReadToEnd());
            }
        }

        private IsolatedStorageFile GetIsolatedStorage()
        {
            if (AppDomain.CurrentDomain.ActivationContext != null)
            {
                try
                {
                    return IsolatedStorageFile.GetUserStoreForApplication();
                }
                catch (Exception)
                {
                }
            }

            // last chance fallback
            return IsolatedStorageFile.GetUserStoreForAssembly();
        }
    }
}
