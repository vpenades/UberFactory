using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.TestPlugins
{
    using UberFactory;    

    [SDK.ContentNode(nameof(AssignIntegerValue))]
    [SDK.Title("Value")]
    public sealed class AssignIntegerValue : SDK.ContentFilter<int>
    {
        [SDK.InputValue(nameof(Value))]        
        public int Value { get; set; }

        protected override int Evaluate() => Value;
    }

    [SDK.ContentNode(nameof(AddIntegerValues))]
    [SDK.Title("A + B")]
    public sealed class AddIntegerValues : SDK.ContentFilter<int>
    {
        [SDK.InputNode(nameof(Value1))]
        public int Value1 { get; set; }

        [SDK.InputNode(nameof(Value2))]
        public int Value2 { get; set; }

        protected override int Evaluate() => Value1 + Value2;
    }

    [SDK.ContentNode(nameof(SubstractIntegerValues))]
    [SDK.Title("A - B")]
    public sealed class SubstractIntegerValues : SDK.ContentFilter<int>
    {
        [SDK.InputNode(nameof(Value1))]
        public int Value1 { get; set; }

        [SDK.InputNode(nameof(Value2))]
        public int Value2 { get; set; }

        protected override int Evaluate() => Value1 - Value2;
    }

    [SDK.ContentNode(nameof(MultiplyIntegerValues))]
    [SDK.Title("A * B")]
    public sealed class MultiplyIntegerValues : SDK.ContentFilter<int>
    {
        [SDK.InputNode(nameof(Value1))]
        public int Value1 { get; set; }

        [SDK.InputNode(nameof(Value2))]
        public int Value2 { get; set; }

        protected override int Evaluate() => Value1 * Value2;
    }

    [SDK.ContentNode(nameof(DivideIntegerValues))]
    [SDK.Title("A / B")]
    public sealed class DivideIntegerValues : SDK.ContentFilter<int>
    {
        [SDK.InputNode(nameof(Value1))]
        public int Value1 { get; set; }

        [SDK.InputNode(nameof(Value2))]
        public int Value2 { get; set; }

        protected override int Evaluate() => Value1 / Value2;
    }






}
