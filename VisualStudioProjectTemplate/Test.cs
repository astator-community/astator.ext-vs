﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using astator.Core.Script;
using $safeprojectname$.Core;

namespace $safeprojectname$;

public class Test
{
    [ScriptEntryMethod(FileName = "Test.cs")]
    public static void Main(ScriptRuntime runtime)
    {
        Runtime.Instance = runtime;
        Console.WriteLine("我是脚本入口方法");
    }
}

