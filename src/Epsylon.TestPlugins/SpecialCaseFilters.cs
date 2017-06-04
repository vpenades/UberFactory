using System;

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
    [SDK.ContentFilterMetaData("Title", "Debug Template Filter")]
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



    [SDK.ContentFilter(nameof(TestGlobalSettings1))]
    [SDK.ContentFilterMetaData("Title", "Debug Shared Settings Filter")]
    public sealed class TestGlobalSettings1 : SDK.ContentFilter<String>
    {

        [SDK.InputValue(nameof(Value1))]
        [SDK.InputMetaDataEvaluate("Default",typeof(MainSettings1),nameof(MainSettings1.Value1))]
        public int Value1 { get; set; }

        protected override String Evaluate()
        {
            var g = this.GetSharedSettings<MainSettings1>();

            var r = Value1 + g?.Value2;

            return r.ToString();
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
