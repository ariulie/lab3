using System;
using System.IO;
using System.Text;
using PhotoEditor.ConsoleApp;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding  = Encoding.UTF8;

string statePath = Path.Combine(AppContext.BaseDirectory, "workspace.json");

new ConsoleSession().Run(Console.In, Console.Out, statePath);