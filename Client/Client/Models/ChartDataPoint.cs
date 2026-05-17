// Models/ChartDataPoint.cs
using System;
namespace Client.Models;

public class ChartDataPoint
{
    public DateTime Time { get; set; }
    public double Intensity { get; set; }
    public int GranuleCount { get; set; }
}