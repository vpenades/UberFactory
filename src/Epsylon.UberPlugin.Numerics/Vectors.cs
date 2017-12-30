using System;
using System.Collections.Generic;
using System.Text;



[assembly: System.Reflection.AssemblyMetadata("SerializationRoot", "UberFactory_Numerics_")]

namespace Epsylon.UberPlugin.Numerics
{
    

    // https://numerics.mathdotnet.com/matrix.html

    using UberFactory;

    using VECTOR = MathNet.Numerics.LinearAlgebra.Double.Vector;



    public abstract class VectorBase : SDK.ContentFilter<VECTOR>
    {
        // the preview is probably failing in the sink because it's a final object

        protected override object EvaluatePreview(SDK.PreviewContext previewContext)
        {
            var result = Evaluate();

            return result == null ? "NULL" : string.Join(" ", result.AsArray());
        }
    }



    [SDK.ContentNode(nameof(VectorSink))]
    [SDK.Title("Vector Sink")]
    public sealed class VectorSink : SDK.ContentFilter
    {
        [SDK.InputNode(nameof(Source))]
        public VECTOR Source { get; set; }

        protected override object EvaluateObject() { return Source; }
    }

    [SDK.ContentNode(nameof(AssignVector3))]
    [SDK.Title("Vector3")]
    public sealed class AssignVector3 : VectorBase
    {
        [SDK.InputValue(nameof(X))]
        [SDK.Title("X")]        
        public Double X { get; set; }

        [SDK.InputValue(nameof(Y))]
        [SDK.Title("Y")]
        public Double Y { get; set; }

        [SDK.InputValue(nameof(Z))]
        [SDK.Title("Z")]
        public Double Z { get; set; }

        protected override VECTOR Evaluate()
        {
            return VectorsFactory.Create(X, Y, Z);
        }        
    }

    [SDK.ContentNode(nameof(AddVector))]
    [SDK.Title("A + B")]
    public sealed class AddVector : VectorBase
    {
        [SDK.InputNode(nameof(A))]
        [SDK.Title("A")]
        public VECTOR A { get; set; }

        [SDK.InputNode(nameof(B))]
        [SDK.Title("B")]
        public VECTOR B { get; set; }

        protected override VECTOR Evaluate()
        {
            return VectorsFactory.Add(A,B);
        }        
    }

    [SDK.ContentNode(nameof(FormatVector))]
    [SDK.Title("Format to Text")]
    public sealed class FormatVector : SDK.ContentFilter<String>
    {
        [SDK.InputNode(nameof(Value))]
        [SDK.Title("Value")]
        public VECTOR Value { get; set; }

        [SDK.InputValue(nameof(Leading))]
        [SDK.Title("Leading")]
        public String Leading { get; set; }

        [SDK.InputValue(nameof(Separator))]
        [SDK.Title("Separator")]
        public String Separator { get; set; }

        [SDK.InputValue(nameof(Trailing))]
        [SDK.Title("Trailing")]
        public String Trailing { get; set; }

        protected override String Evaluate()
        {
            var sequence = Value==null? string.Empty : String.Join(Separator, Value.AsArray());

            // todo: handle CultureInfo
            return $"{Leading}{sequence}{Trailing}";
        }
    }
}
