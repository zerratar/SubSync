using System;

namespace SubSync
{

    internal interface IStatusResultReporter<TReportResult> : IStatusReporter
    {
        event EventHandler<TReportResult> OnReportFinished;
    }
    
    internal interface IStatusReporter<in TReportData>  : IStatusReporter
    {        
        void Report(TReportData data);        
    }

    internal interface IStatusReporter
    {
        void FinishReport();
    }
}