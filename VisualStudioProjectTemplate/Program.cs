namespace $safeprojectname$;

public class Program
{
    [ProjectEntryMethod(IsUIMode = false)]
    public static void Main(ScriptRuntime runtime)
    {
        Runtime.Instance = runtime;
        Console.WriteLine("我是项目入口方法");
    }
}

