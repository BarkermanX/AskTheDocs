
namespace AskTheDocs.Services;

public class CosineSimilarityService
{
    public double Calculate(float[] floatVectorA, float[] floatVectorB)
    {
        if (floatVectorA.Length != floatVectorB.Length)
        {
            throw new ArgumentException(
                "Vectors must have the same number of dimensions.");
        }

        double dblDotProduct = 0;
        double dblMagnitudeA = 0;
        double dblMagnitudeB = 0;

        for (int i = 0; i < floatVectorA.Length; i++)
        {
            dblDotProduct += floatVectorA[i] * floatVectorB[i];

            dblMagnitudeA += floatVectorA[i] * floatVectorA[i];
            dblMagnitudeB += floatVectorB[i] * floatVectorB[i];
        }

        if (dblMagnitudeA == 0 || dblMagnitudeB == 0)
        {
            throw new ArgumentException(
                "Vectors must not be zero vectors.");
        }

        return dblDotProduct /
            (Math.Sqrt(dblMagnitudeA) * Math.Sqrt(dblMagnitudeB));
    }
}
