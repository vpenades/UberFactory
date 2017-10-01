using System;

namespace Epsylon.TestPlugins
{
    using UberFactory;

    [SDK.ContentNode(nameof(TestSlowProgressBar))]
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

                System.Threading.Tasks.Task.Delay(waitTime).Wait();
            }

            return string.Empty;
        }
    }
        
    [SDK.ContentNode(nameof(InvalidNodeTest))]
    public sealed class InvalidNodeTest : SDK.ContentFilter<String>
    {
        /// <summary>
        /// We've chosen a return type that it's going to be rarely used as a interoperation type to throw
        /// </summary>
        [SDK.InputNode(nameof(InvalidType))]
        public System.Attribute InvalidType { get; set; }

        [SDK.InputValue(nameof(InvalidArray))]
        public int[][,] InvalidArray { get; set; }

        [SDK.InputValue(nameof(Date))]
        public DateTime Date { get; set; }

        protected override String Evaluate() { return string.Empty; }
    }
}
