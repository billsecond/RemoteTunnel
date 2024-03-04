using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public static class EncryptService
    {
        static byte[] cipherToPlainArr;
        static byte[] plainToCipherArr;
        static int threshold;
        static public void Init()
        {
            byte[] arr = new byte[256];
            byte[] arr1 = new byte[256];
            for (int i = 0; i <= 0xff; i++)
            {
                byte v;
                if (i + 0x30 <= 0xff)
                    v = (byte)(i + 0x30);
                else
                    v = (byte)(i + 0x30 - 0xff - 1);

                arr[i] = v;
                arr1[v] = (byte)i;
            }
            EncryptService.plainToCipherArr = arr;
            EncryptService.cipherToPlainArr = arr1;
            EncryptService.threshold = 100;
        }
        static public byte[] Encrypt(byte[] buffer)
        {
            int reach = Math.Min(EncryptService.threshold, buffer.Length);
            for (int i = 0; i < reach; i++)
            {
                buffer[i] = EncryptService.plainToCipherArr[buffer[i]];
            }
            return buffer;
        }
        static public byte[] Decrypt(byte[] buffer)
        {

            int reach = Math.Min(EncryptService.threshold, buffer.Length);
            for (int i = 0; i < reach; i++)
            {
                buffer[i] = EncryptService.cipherToPlainArr[buffer[i]];
            }
            return buffer;
        }
    }
}
