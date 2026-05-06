using AcmeUI.Models;
using System;
using System.Collections.ObjectModel;

public static class GaussianFitService
{
    public static void FitAllSeries(ObservableCollection<ScanRow> rows)
    {
        int n = rows.Count;
        if (n < 5) return;

        // Estrai array X e le 5 serie
        double[] x = new double[n];
        double[] b1 = new double[n];
        double[] b2 = new double[n];
        double[] b3 = new double[n];
        double[] b4 = new double[n];
        double[] b5 = new double[n];

        for (int i = 0; i < n; i++)
        {
            x[i] = rows[i].Position;
            b1[i] = rows[i].B1;
            b2[i] = rows[i].B2;
            b3[i] = rows[i].B3;
            b4[i] = rows[i].B4;
            b5[i] = rows[i].B5;
        }

        // Fit delle 5 serie
        double[] f1 = FitGaussian(x, b1);
        double[] f2 = FitGaussian(x, b2);
        double[] f3 = FitGaussian(x, b3);
        double[] f4 = FitGaussian(x, b4);
        double[] f5 = FitGaussian(x, b5);

        // Aggiorna la collection
        for (int i = 0; i < n; i++)
        {
            rows[i].B1 = (float)f1[i];
            rows[i].B2 = (float)f2[i];
            rows[i].B3 = (float)f3[i];
            rows[i].B4 = (float)f4[i];
            rows[i].B5 = (float)f5[i];
        }
    }

    // ------------------------------
    // Fit gaussiano per una singola serie
    // ------------------------------
    private static double[] FitGaussian(double[] x, double[] B)
    {
        int n = x.Length;
        double[] Bfit = new double[n];

        // --- 1) Stima iniziale ---
        double A = double.MinValue;
        int idxMax = 0;

        for (int i = 0; i < n; i++)
        {
            if (B[i] > A)
            {
                A = B[i];
                idxMax = i;
            }
        }

        double Mu = x[idxMax];

        // Stima sigma tramite FWHM
        double half = A * 0.5;
        double xL = Mu, xR = Mu;

        for (int i = 0; i < n; i++)
        {
            if (B[i] < half && x[i] < Mu)
                xL = x[i];
            if (B[i] < half && x[i] > Mu)
            {
                xR = x[i];
                break;
            }
        }

        double Sigma = (xR - xL) / 2.355;
        if (Sigma <= 0) Sigma = (x[n - 1] - x[0]) / 6.0;

        // --- 2) Iterazioni Gauss–Newton ---
        for (int iter = 0; iter < 10; iter++)
        {
            double dA = 0, dMu = 0, dSigma = 0;

            for (int i = 0; i < n; i++)
            {
                double dx = x[i] - Mu;
                double e = Math.Exp(-(dx * dx) / (2 * Sigma * Sigma));
                double G = A * e;
                double err = B[i] - G;

                dA += err * e;
                dMu += err * G * (dx / (Sigma * Sigma));
                dSigma += err * G * (dx * dx / (Sigma * Sigma * Sigma));
            }

            A += 0.001 * dA;
            Mu += 0.001 * dMu;
            Sigma += 0.001 * dSigma;

            if (Sigma < 1e-6) Sigma = 1e-6;
        }

        // --- 3) Calcolo valori fittati ---
        for (int i = 0; i < n; i++)
        {
            double dx = x[i] - Mu;
            Bfit[i] = A * Math.Exp(-(dx * dx) / (2 * Sigma * Sigma));
        }

        return Bfit;
    }
}

