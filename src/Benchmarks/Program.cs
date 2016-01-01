﻿// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Threading;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;

namespace Benchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("ASP.NET 5 Benchmarks");
            Console.WriteLine("--------------------");

            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();

            if (string.IsNullOrEmpty(config["scenarios"]))
            {
                Console.WriteLine("Which scenarios would you like to enable?:");
                Console.WriteLine("  1: Raw middleware");
                Console.WriteLine("  2: All MV");
                Console.Write("1, 2, 3, A(ll)> ");
            }

            var app = new WebApplicationBuilder()
                .UseConfiguration(WebApplicationConfiguration.GetDefault(args))
                .UseStartup<Startup>()
                .Build();

            // Run the interaction on a separate thread as we don't have Console.KeyAvailable on .NET Core so can't
            // do a pre-emptive check before we call Console.ReadKey (which blocks, hard)
            var interactiveThread = new Thread(() =>
            {
                Console.WriteLine();
                Console.WriteLine("Press 'C' to force GC or any other key to display GC stats");

                while (true)
                {
                    var key = Console.ReadKey(intercept: true);

                    if (key.Key == ConsoleKey.C)
                    {
                        Console.WriteLine();
                        Console.Write("Forcing GC...");
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        GC.Collect();
                        Console.WriteLine(" done!");
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.WriteLine($"Allocated: {GetAllocatedMemory()}");
                        Console.WriteLine($"Gen 0: {GC.CollectionCount(0)}, Gen 1: {GC.CollectionCount(1)}, Gen 2: {GC.CollectionCount(2)}");
                    }
                }
            });
            interactiveThread.IsBackground = true;
            interactiveThread.Start();

            app.Run();
        }

        private static string GetAllocatedMemory(bool forceFullCollection = false)
        {
            double bytes = GC.GetTotalMemory(forceFullCollection);

            return $"{((bytes / 1024d) / 1024d).ToString("N2")} MB";
        }
    }
}
