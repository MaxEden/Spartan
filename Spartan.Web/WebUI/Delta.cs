using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Spartan.Web.WebUI
{
    internal class Delta
    {
        public static byte[] GetDeltaSimd(WebBlitter _blitter, byte[] lastSend, out bool send, out int size, out int countStartSame, out int countEndSame, out byte[] sendBytes)
        {
            sendBytes = _blitter.WrittenBytes.ToArray();

            send = true;
            countStartSame = 0;
            countEndSame = 0;

            if (lastSend != null)
            {
                int n = Math.Min(lastSend.Length, sendBytes.Length);

                int lsize = Vector<byte>.Count;

                ref byte refSend = ref MemoryMarshal.GetArrayDataReference(sendBytes);
                ref byte refLast = ref MemoryMarshal.GetArrayDataReference(lastSend);

                var shift1 = sendBytes.Length % lsize;
                var shift2 = lastSend.Length % lsize;

                ref byte refSendEnd = ref Unsafe.Add(ref refSend, shift1);
                ref byte refLastEnd = ref Unsafe.Add(ref refLast, shift2);

                int refSendEndSize = sendBytes.Length - shift1;
                int refLastEndSize = lastSend.Length - shift2;

                int ln = n / lsize; //Math.Min(lastSend.Length / lsize, sendBytes.Length / lsize);

                int li = 0;//by longs
                for (; li < ln; li++)
                {
                    ref byte sht = ref Unsafe.Add(ref refSend, li * lsize);
                    ref byte sht2 = ref Unsafe.Add(ref refLast, li * lsize);

                    ref Vector<byte> long1 = ref Unsafe.As<byte, Vector<byte>>(ref sht);
                    ref Vector<byte> long2 = ref Unsafe.As<byte, Vector<byte>>(ref sht2);

                    if (long1 == long2)
                    {
                        countStartSame += lsize;
                    }
                    else
                    {
                        break;
                    }
                }

                //by bytes
                for (int i = li * lsize; i < n; i++)
                {
                    if (lastSend[i] == sendBytes[i])
                    {
                        countStartSame++;
                    }
                    else break;
                }

                //if not same
                if (countStartSame < n)
                {
                    int endN = n - countStartSame;
                    int endNL = endN / lsize;

                    li = 1;//by longs aligned
                    for (; li < endNL; li++)
                    {
                        ref byte sht = ref Unsafe.Add(ref refSendEnd, refSendEndSize - li * lsize);
                        ref byte sht2 = ref Unsafe.Add(ref refLastEnd, refLastEndSize - li * lsize);

                        ref Vector<byte> long1 = ref Unsafe.As<byte, Vector<byte>>(ref sht);
                        ref Vector<byte> long2 = ref Unsafe.As<byte, Vector<byte>>(ref sht2);

                        if (long1 == long2)
                        {
                            countEndSame += lsize;
                        }
                        else break;
                    }

                    //by bytes aligned
                    var prevLi = li - 1;
                    var lastCheckedI = prevLi * lsize;

                    for (int i = lastCheckedI + 1; i < endN; i++)
                    {
                        if (lastSend[^i] == sendBytes[^i])
                        {
                            countEndSame++;
                        }
                        else break;
                    }
                }
            }

            if (countStartSame == sendBytes.Length)
            {
                send = false;
                size = 0;
                return null;
            }

            Debug.WriteLine($"{sendBytes.Length} {countEndSame} {countStartSame}");

            //int lastSize = lastSend.Length - countEndSame - countStartSame;



            size = sendBytes.Length - countEndSame - countStartSame;
            if (size < 0)
            {//????
                countStartSame = 0;
                countEndSame = 0;
                size = sendBytes.Length;
            }


            //int minSize = Math.Min(lastSize, size);

            //if (minSize > Vector<byte>.Count)
            //{

            //    ref byte refSend = ref MemoryMarshal.GetArrayDataReference(sendBytes);
            //    ref byte refLast = ref MemoryMarshal.GetArrayDataReference(lastSend);

            //    for (int i = countStartSame; i < countStartSame + lastSize - Vector<byte>.Count; i++)
            //    {
            //        ref byte checkRef = ref Unsafe.Add(ref refLast, i);
            //        for (int j = countStartSame; j < countStartSame + size - Vector<byte>.Count; i++)
            //        {
            //            ref byte check2Ref = ref Unsafe.Add(ref refLast, j);
            //            if(check2Ref == checkRef)
            //            {

            //            }
            //        }
            //    }
            //}

            var span = new byte[size];
            Array.Copy(sendBytes, countStartSame, span, 0, size);
            return span;
        }

    }
}
