# Hypervisor
_"Power doesn't come from size, but rather, from intelligence."_

As the description suggests, Hypervisor is a teeny-tiny memory library for C#, utilizing the power of Marshaling to achieve clean code.
It does not have a separate function for every type imaginable, unlike all the other memory libraries, but rather has one generic function for each purpose.

## Attaching to a Process

Hypervisor is a _parasite library_, meaning it needs to attach to a process in order to work. You can attach a process like so:

```csharp
var _myProcess = Process.GetProcessesByName("MY_PROCESS")[0];
Hypervisor.AttachProcess(_myProcess);
```

_This is only given as an example, you may fetch your own process in however you like._

## Reading and Writing

To read a value in memory, all you have to do is to say what address you want to read from, and tell the type!

```csharp
int _someInt = Hypervisor.Read(0xCAFEEFAC);
float _someFloat = Hypervisor.Read(0xDEADBEEF);
```

of if you are reading an array, tell the size:

```csharp
var _someByteArray = Hypervisor.Read<byte>(0xCAFEEFAC, 0x04);
var _someUShortArray = Hypervisor.Read<ushort>(0xDEADBEEF, 0x04);
```

The same goes for writing, of course:

```csharp
Hypervisor.Write<int>(0xCAFEEFAC, 0xDEAD);
Hypervisor.Write<byte>(0xDEADBEEF, [0x04, 0x05, 0x06, 0x07]);
```

That's literally it!

## Installation

Just add the file to your project and adjust it's namespace. You don't need to do anything else (Well, you will also need to enable unsafe code because Marshal). Plug and play!
