using System;

namespace Rias.Commons
{
    public class EvaluationDetails
    {
        public TimeSpan? CompilationTime { get; set; }
        
        public TimeSpan? ExecutionTime { get; set; }
        
        public string? Code { get; set; }
        
        public string? Result { get; set; }
        
        public string? ReturnType { get; set; }
        
        public bool IsCompiled { get; set; }
        
        public bool Success { get; set; }
        
        public string? Exception { get; set; }
    }
}