using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Hypervisor
{
    public static class Hypervisor
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, uint flNewProtect, ref int lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        static IntPtr Handle;
        public static Process Process;
        public static ulong PureAddress;
        public static ulong MemoryOffset;

        static byte[]? _patternBuffer = null;

        /// <summary>
        /// Initialize the Hypervisor on a process.
        /// </summary>
        /// <param name="Input">The input process.</param>
        public static void AttachProcess(Process Input)
        {
            Process = Input;
            Handle = Input.Handle;
            PureAddress = (ulong)Input.MainModule.BaseAddress;
            MemoryOffset = PureAddress & 0x7FFF00000000;
        }

        /// <summary>
        /// Reads a value with the type of T from an address.
        /// Unsafe, must be used with caution.
        /// </summary>
        /// <typeparam name="T">Type of the value to read.</typeparam>
        /// <param name="Address">The address of the value to read.</param>
        /// <returns>The value as it is read from memory.</returns>
        public static T Read<T>(ulong Address) where T : struct
        {
            var _dynoMethod = new DynamicMethod("SizeOfType", typeof(int), new Type[] { });
            ILGenerator _ilGen = _dynoMethod.GetILGenerator();

            _ilGen.Emit(OpCodes.Sizeof, typeof(T));
            _ilGen.Emit(OpCodes.Ret);

            var _outSize = (int)_dynoMethod.Invoke(null, null);

            var _outArray = new byte[_outSize];
            int _outRead = 0;

            ReadProcessMemory(Handle, (IntPtr)(PureAddress + Address), _outArray, _outSize, ref _outRead);

            var _gcHandle = GCHandle.Alloc(_outArray, GCHandleType.Pinned);
            var _retData = (T)Marshal.PtrToStructure(_gcHandle.AddrOfPinnedObject(), typeof(T));

            _gcHandle.Free();

            return _retData;
        }

        /// <summary>
        /// Reads an array with the type of T[] from an address.
        /// Unsafe, must be used with caution.
        /// </summary>
        /// <typeparam name="T">Type of the array to read.</typeparam>
        /// <param name="Address">The address of the value to read.</param>
        /// <param name="Size">The size of the array to read.</param>
        /// <returns>The array as it is read from memory.</returns>
        public static T[] Read<T>(ulong Address, int Size) where T : struct
        {
            var _dynoMethod = new DynamicMethod("SizeOfType", typeof(int), new Type[] { });
            ILGenerator _ilGen = _dynoMethod.GetILGenerator();

            _ilGen.Emit(OpCodes.Sizeof, typeof(T));
            _ilGen.Emit(OpCodes.Ret);

            var _outSize = (int)_dynoMethod.Invoke(null, null);

            var _outArray = new byte[Size * _outSize];
            int _outRead = 0;

            ReadProcessMemory(Handle, (IntPtr)(PureAddress + Address), _outArray, Size * _outSize, ref _outRead);

            var _retArray = new T[Size];

            for (int i = 0; i < Size; i++)
            {
                var _pickArray = _outArray.Skip(i * _outSize).Take(_outSize).ToArray();
                
                var _gcHandle = GCHandle.Alloc(_pickArray, GCHandleType.Pinned);
                var _convData = (T)Marshal.PtrToStructure(_gcHandle.AddrOfPinnedObject(), typeof(T));

                _retArray[i] = _convData;
                _gcHandle.Free();
            }

            return _retArray;
        }

        /// <summary>
        /// Writes a value with the type of T to an address.
        /// Unsafe, must be used with caution.
        /// </summary>
        /// <typeparam name="T">Type of the value to write. Must have a size.</typeparam>
        /// <param name="Address">The address which the value will be written to.</param>
        /// <param name="Value">The value to write.</param>
        public static void Write<T>(ulong Address, T Value) where T : struct
        {
            var _dynoMethod = new DynamicMethod("SizeOfType", typeof(int), new Type[] { });
            ILGenerator _ilGen = _dynoMethod.GetILGenerator();

            _ilGen.Emit(OpCodes.Sizeof, typeof(T));
            _ilGen.Emit(OpCodes.Ret);

            var _inSize = (int)_dynoMethod.Invoke(null, null);
            int _inWrite = 0;

            if (_inSize > 1)
            {
                var _inArray = (byte[])typeof(BitConverter).GetMethod("GetBytes", new[] { typeof(T) }).Invoke(null, new object[] { Value });
                WriteProcessMemory(Handle, (IntPtr)Address, _inArray, _inArray.Length, ref _inWrite);
            }

            else
            {
                var _inArray = new byte[] { (byte)Convert.ChangeType(Value, typeof(byte)) };
                WriteProcessMemory(Handle, (IntPtr)Address, _inArray, _inArray.Length, ref _inWrite);
            }
        }

        /// <summary>
        /// Writes an array with the type of T to an address.
        /// Unsafe, must be used with caution.
        /// </summary>
        /// <typeparam name="T">Type of the array to write. Must have a size.</typeparam>
        /// <param name="Address">The address which the Array will be written to.</param>
        /// <param name="Value">The array to write.</param>
        public static void Write<T>(ulong Address, T[] Value) where T : struct
        {
            var _dynoMethod = new DynamicMethod("SizeOfType", typeof(int), new Type[] { });
            ILGenerator _ilGen = _dynoMethod.GetILGenerator();

            _ilGen.Emit(OpCodes.Sizeof, typeof(T));
            _ilGen.Emit(OpCodes.Ret);

            var _inSize = (int)_dynoMethod.Invoke(null, null);
            int _inWrite = 0;

            if (_inSize > 1)
            {
                for (int i = 0; i < Value.Length; i++)
                {
                    var _inArray = (byte[])typeof(BitConverter).GetMethod("GetBytes", [typeof(T)]).Invoke(null, [Value[i]]);
                    WriteProcessMemory(Handle, (IntPtr)(PureAddress + Address) + _inSize * i, _inArray, _inArray.Length, ref _inWrite);
                }
            }

            else
                WriteProcessMemory(Handle, (IntPtr)(PureAddress + Address), Value as byte[], Value.Length, ref _inWrite);
        }

        /// <summary>
        /// Unlocks a particular block to be written.
        /// </summary>
        /// <param name="Address">The address of the subject block.</param>
        public static void UnlockBlock(ulong Address)
        {
            int _oldProtect = 0;
            VirtualProtectEx(Handle, (IntPtr)Address, 0x100000, 0x40, ref _oldProtect);
        }

        /// <summary>
        /// Checks if the pointer exists, is valid, and isn't repurposed.
        /// </summary>
        /// <param name="Address">The address.</param>
        /// <returns>TRUE if it is valid, FALSE otherwise.</returns>
        public static bool IsValidPointer(this ulong Address)
        {
            var _readValue = Read<ulong>(Address);
            if (_readValue == 0x00 || _readValue == 0xEFACCAFE || _readValue == 0xCAFEEFAC || _readValue == 0xFFFFFFFF)
                return false;
            else
                return true;
        }
    }
}
