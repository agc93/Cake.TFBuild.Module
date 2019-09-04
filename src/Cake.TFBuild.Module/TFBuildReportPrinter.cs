﻿using System;
using System.IO;
using System.Text;
using Cake.Common.Build;
using Cake.Core;
using Cake.Core.IO;
using Cake.Module.Shared;

namespace Cake.TFBuild.Module
{
    /// <summary>
    /// The TF Build/Azure Pipelines report printer.
    /// </summary>
    public class TFBuildReportPrinter : CakeReportPrinterBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TFBuildReportPrinter"/> class.
        /// </summary>
        /// <param name="console">The console.</param>
        /// <param name="context">The context.</param>
        public TFBuildReportPrinter(IConsole console, ICakeContext context) : base(console, context)
        {
        }

        /// <summary>
        /// Writes the specified report to a target.
        /// </summary>
        /// <param name="report">The report to write.</param>
        public override void Write(CakeReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            try
            {

                if (_context.TFBuild().IsRunningOnAzurePipelines || _context.TFBuild().IsRunningOnAzurePipelinesHosted) {
                    WriteToMarkdown(report);
                }
                WriteToConsole(report);
            }
            finally
            {
                _console.ResetColor();
            }
        }

        private void WriteToMarkdown(CakeReport report)
        {
                var maxTaskNameLength = 29;
                foreach (var item in report)
                {
                    if (item.TaskName.Length > maxTaskNameLength)
                    {
                        maxTaskNameLength = item.TaskName.Length;
                    }
                }

                maxTaskNameLength++;
            string lineFormat = "|{0,-" + maxTaskNameLength + "}|{1,20}|";


            var sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine("|Task|Duration|");
            sb.AppendLine("|:---|-------:|");
            foreach (var item in report)
            {
                if (ShouldWriteTask(item))
                {
                    sb.AppendLine(string.Format(lineFormat, item.TaskName, FormatDuration(item)));
                }
            }
            sb.AppendLine("");
            var b = _context.BuildSystem().TFBuild;
            FilePath agentWorkPath = b.Environment.Build.ArtifactStagingDirectory + "/tasksummary.md";
            var absFilePath = agentWorkPath.MakeAbsolute(_context.Environment);
            var file = _context.FileSystem.GetFile(absFilePath);
            using (var writer = new StreamWriter(file.OpenWrite())) {
                writer.Write(sb.ToString());
            }
            //b.Commands.UploadTaskSummary(absFilePath);
            _console.WriteLine($"##vso[task.addattachment type=Distributedtask.Core.Summary;name=Cake Build Summary;]{absFilePath.MakeAbsolute(_context.Environment).FullPath}");
        }
    }
}