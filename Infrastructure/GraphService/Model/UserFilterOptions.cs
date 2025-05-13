namespace AzureExternalDirectory.Infrastructure.GraphService.Model
{
    public class UserFilterOptions
    {
        public int? Top { get; set; }
        public string Filter { get; set; }
        public string Search { get; set; }
        public string OrderBy { get; set; }
        
        public GraphQueryOptions QueryOptions { get; set; }
        
        public GraphQueryOptions GetQueryOptionsOrDefault()
        {
            return QueryOptions ?? new GraphQueryOptions();
        }
    }
}