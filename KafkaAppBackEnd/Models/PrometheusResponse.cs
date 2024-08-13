namespace KafkaAppBackEnd.Models
{
    public class Data
    {
        public string resultType { get; set; }
        public List<Result> result { get; set; }
    }

    public class Metric
    {
        public string __name__ { get; set; }
        public string instance { get; set; }
        public string job { get; set; }
        public string partition { get; set; }
        public string topic { get; set; }
    }

    public class Result
    {
        public Metric metric { get; set; }
        public List<object> value { get; set; }
    }

    public class PrometheusResponse
    {
        public string status { get; set; }
        public Data data { get; set; }
    }
}
