using System;
using System.Collections.ObjectModel;
using AcmeUI.Models;

namespace AcmeUI.Services
{
    public sealed class IntegrationService
    {
        private static readonly Lazy<IntegrationService> _instance =
            new(() => new IntegrationService());

        public static IntegrationService Instance => _instance.Value;

        private IntegrationService() { }

        // ---------------------------------------------------------
        // IntB: integrazione numerica (metodo dei trapezi)
        // ---------------------------------------------------------
        public float[] IntB(ObservableCollection<ScanRow> rows)
        {
            float[] result = new float[5];

            if (rows == null || rows.Count < 2)
                return result;

            for (int i = 0; i < rows.Count - 1; i++)
            {
                var r0 = rows[i];
                var r1 = rows[i + 1];

                float dx = r1.Position - r0.Position;
                if (dx <= 0)
                    continue; // ignora posizioni non crescenti

                result[0] += 0.5f * (r0.B1 + r1.B1) * dx;
                result[1] += 0.5f * (r0.B2 + r1.B2) * dx;
                result[2] += 0.5f * (r0.B3 + r1.B3) * dx;
                result[3] += 0.5f * (r0.B4 + r1.B4) * dx;
                result[4] += 0.5f * (r0.B5 + r1.B5) * dx;
            }

            return result;
        }

        // ---------------------------------------------------------
        // DeltaIntB: normalizzazione rispetto a refIndex
        // ---------------------------------------------------------
        public float[] DeltaIntB(ObservableCollection<ScanRow> rows, int refIndex)
        {
            if (refIndex < 0 || refIndex > 4)
                throw new ArgumentOutOfRangeException(nameof(refIndex));

            float[] intB = IntB(rows);

            float refValue = intB[refIndex];

            // Se il riferimento è zero → ritorna vettore di zeri
            if (refValue == 0)
                return new float[5];

            float[] result = new float[5];

            for (int i = 0; i < 5; i++)
                result[i] = (intB[i] - refValue) / refValue;

            return result;
        }
    }
}
