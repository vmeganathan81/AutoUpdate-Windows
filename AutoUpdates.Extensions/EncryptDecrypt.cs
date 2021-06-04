using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace AutoUpdates.Extensions
{
    public class EncryptDecrypt
    {
        public static void EncryptFile(string sInputFilename,
           string sOutputFilename,
           string sPassword)
        {
            try
            {
                UnicodeEncoding UE = new UnicodeEncoding();
                byte[] key = UE.GetBytes(sPassword);

                string cryptFile = sOutputFilename;
                using (FileStream fsCrypt = new FileStream(cryptFile, FileMode.Create))
                {
                    RijndaelManaged RMCrypto = new RijndaelManaged();

                    using (CryptoStream cs = new CryptoStream(fsCrypt,
                        RMCrypto.CreateEncryptor(key, key),
                        CryptoStreamMode.Write))
                    {

                        using (FileStream fsIn = new FileStream(sInputFilename, FileMode.Open))
                        {
                            int data;
                            while ((data = fsIn.ReadByte()) != -1)
                                cs.WriteByte((byte)data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //throw ex;
            }
        }


         public static void DecryptFile(string sInputFilename,
            string sOutputFilename,
            string sPassword)
         {
             try
             {

                 UnicodeEncoding UE = new UnicodeEncoding();
                 byte[] key = UE.GetBytes(sPassword);

                 using (FileStream fsCrypt = new FileStream(sInputFilename, FileMode.Open))
                 {

                     RijndaelManaged RMCrypto = new RijndaelManaged();
                     using (CryptoStream cs = new CryptoStream(fsCrypt,
                                             RMCrypto.CreateDecryptor(key, key),
                                             CryptoStreamMode.Read))
                     {
                         using (FileStream fsOut = new FileStream(sOutputFilename, FileMode.Create))
                         {
                             int data;
                             while ((data = cs.ReadByte()) != -1)
                                 fsOut.WriteByte((byte)data);
                         }
                     }
                 }

             }
             catch (Exception ex)
             {
                // throw ex;
             }
         }
    }
}
