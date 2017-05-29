﻿using System;

namespace Epsylon.TestPlugins
{
    using UberFactory;

    [SDK.ContentFilter(nameof(TestSlowProgressBar))]
    public sealed class TestSlowProgressBar : SDK.ContentFilter<String>
    {
        [SDK.InputValue(nameof(PauseTime))]
        [SDK.InputMetaData("Title","Pause Time in Seconds")]
        [SDK.InputMetaData("Default",10)]
        [SDK.InputMetaData("Minimum", 1)]
        public int PauseTime { get; set; }

        protected override String Evaluate()
        {
            var waitTime = TimeSpan.FromSeconds( (double)PauseTime / 100 );

            for (int i = 0; i < 100; ++i)
            {
                this.SetProgressPercent(i);
                System.Threading.Thread.Sleep(waitTime);
            }

            return string.Empty;
        }
    }

    [SDK.ContentFilter(nameof(TestPipeline1))]
    [SDK.ContentFilterMetaData("Title", "Debug Content Filter")]
    public sealed class TestPipeline1 : SDK.ContentFilter<String>
    {
        [SDK.InputPipeline(nameof(Pipeline), typeof(string), typeof(string),typeof(string),typeof(string))]        

        // Func<string,string,string,string>
        public SDK.IPipelineInstance Pipeline { get; set; }

        protected override String Evaluate()
        {
            return null;

            // return Pipeline.Evaluate(MonitorContext.CreateNull(), "A", "B", "C") as String;
        }
    }

    [SDK.ContentFilter(nameof(InvalidNodeTest))]
    public sealed class InvalidNodeTest : SDK.ContentFilter<String>
    {
        [SDK.InputNode(nameof(InvalidType))]
        public System.ResolveEventArgs InvalidType { get; set; }

        [SDK.InputValue(nameof(Date))]
        public DateTime Date { get; set; }

        protected override String Evaluate() { return string.Empty; }
    }
}
