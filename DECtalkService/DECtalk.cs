using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace CaaS.Service.DECtalk
{
    public static class DECtalk
    {
        [CaaSEndpoint("/dectalk", "audio/wav")]
        public static byte[] Endpoint(string input)
        {
            string tmpfile = Path.Combine(Path.GetTempPath(), "/dectalk.wav");
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = Path.Combine(CaaSHost.Root, "bin/say.exe"),
                Arguments = "-pre \"[:phone on]\" -w \"" + tmpfile + "\"",
                WorkingDirectory = Path.Combine(CaaSHost.Root, "bin"),
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process proc = Process.Start(psi);
            proc.StandardInput.Write(input);
            proc.StandardInput.Close();
            proc.WaitForExit();
            byte[] buffer = File.ReadAllBytes(tmpfile);
            return buffer;
        }
    }
}
