extern "C" int __managed__Main(int argc, wchar_t* argv[]);

int __cdecl wmain(int argc, wchar_t* argv[])
{
    return __managed__Main(argc, argv);
}