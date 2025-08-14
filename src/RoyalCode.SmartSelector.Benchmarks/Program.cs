// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Running;

Console.WriteLine("Hello, World!");

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);