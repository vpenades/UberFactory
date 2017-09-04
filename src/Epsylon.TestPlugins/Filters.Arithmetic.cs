using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Epsylon.TestPlugins
{
    using UberFactory;

    [SDK.ContentNode(nameof(AssignIntegerValue))]
    public sealed class AssignIntegerValue : SDK.ContentFilter<int>
    {
        [SDK.InputValue(nameof(Value))]        
        public int Value { get; set; }

        protected override int Evaluate() => Value;
    }

    [SDK.ContentNode(nameof(AddIntegerValues))]
    public sealed class AddIntegerValues : SDK.ContentFilter<int>
    {
        [SDK.InputNode(nameof(Value1))]
        public int Value1 { get; set; }

        [SDK.InputNode(nameof(Value2))]
        public int Value2 { get; set; }

        protected override int Evaluate() => Value1 + Value2;
    }

    [SDK.ContentNode(nameof(SubstractIntegerValues))]
    public sealed class SubstractIntegerValues : SDK.ContentFilter<int>
    {
        [SDK.InputNode(nameof(Value1))]
        public int Value1 { get; set; }

        [SDK.InputNode(nameof(Value2))]
        public int Value2 { get; set; }

        protected override int Evaluate() => Value1 - Value2;
    }

    [SDK.ContentNode(nameof(MultiplyIntegerValues))]
    public sealed class MultiplyIntegerValues : SDK.ContentFilter<int>
    {
        [SDK.InputNode(nameof(Value1))]
        public int Value1 { get; set; }

        [SDK.InputNode(nameof(Value2))]
        public int Value2 { get; set; }

        protected override int Evaluate() => Value1 * Value2;
    }

    [SDK.ContentNode(nameof(DivideIntegerValues))]
    public sealed class DivideIntegerValues : SDK.ContentFilter<int>
    {
        [SDK.InputNode(nameof(Value1))]
        public int Value1 { get; set; }

        [SDK.InputNode(nameof(Value2))]
        public int Value2 { get; set; }

        protected override int Evaluate() => Value1 / Value2;
    }






}
