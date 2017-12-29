using System;
using System.Collections.Generic;
using System.Text;



[assembly: System.Reflection.AssemblyMetadata("SerializationRoot", "UberFactory_Numerics_")]

namespace Epsylon.UberPlugin.Numerics
{
    

    // https://numerics.mathdotnet.com/matrix.html

    using UberFactory;

    using Vector3 = MathNet.Numerics.LinearAlgebra.Double.Vector;



    public abstract class Vector3Base : SDK.ContentFilter<Vector3>
    {
        protected override object EvaluatePreview(SDK.PreviewContext previewContext)
        {
            var result = Evaluate();

            return $"{result[0]} {result[1]} {result[2]}";
        }
    }



    [SDK.ContentNode(nameof(Vector3Sink))]
    [SDK.Title("Vector3 Sink")]
    public sealed class Vector3Sink : SDK.ContentFilter
    {
        [SDK.InputNode(nameof(Source))]
        public Vector3 Source { get; set; }

        protected override object EvaluateObject()
        {
            return null;
        }
    }

    [SDK.ContentNode(nameof(AssignVector3))]
    [SDK.Title("Vector3")]
    public sealed class AssignVector3 : Vector3Base
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

        protected override Vector3 Evaluate()
        {
            return Vector3.Build.DenseOfArray(new double[] { X, Y, Z }) as Vector3;
        }        
    }

    [SDK.ContentNode(nameof(AddVector3))]
    [SDK.Title("A + B")]
    public sealed class AddVector3 : Vector3Base
    {
        [SDK.InputNode(nameof(A))]
        [SDK.Title("A")]
        public Vector3 A { get; set; }

        [SDK.InputNode(nameof(B))]
        [SDK.Title("B")]
        public Vector3 B { get; set; }

        protected override Vector3 Evaluate()
        {
            return A.Add(B) as Vector3;
        }        
    }
}
