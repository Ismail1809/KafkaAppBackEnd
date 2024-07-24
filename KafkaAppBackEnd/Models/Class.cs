namespace KafkaAppBackEnd.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }
        public string Username { get; set; }
        public string Location { get; set; }
    }

    public class Account
    {
        public string AccountId { get; set; }
        public int CustomerId { get; set; }
        public string Balance { get; set; }
    }

    public class CustomerAccount
    {
        public int CustomerId { get; set; }
        public string Username { get; set; }
        public string Location { get; set; }
        public string AccountId { get; set; }
        public string Balance { get; set; }

        public override string ToString()
        {
            return $"{CustomerId}\t{Username}\t{Location}\t{AccountId}\t{Balance}";
        }
    }

    public class Worker
    {
        public void Work()
        {
            var customers = new List<Customer>()
            {
                new Customer() { CustomerId = 1, Username = "ism11", Location = "Baku" },
                new Customer() { CustomerId = 2, Username = "asif21", Location = "Moscow" },
                new Customer() { CustomerId = 3, Username = "amina87", Location = "London" }
            };

            var accounts = new List<Account>()
            {
                new Account() { AccountId = "aaa", CustomerId = 1, Balance = "1000" },
                new Account() { AccountId = "bbb", CustomerId = 3, Balance = "500" }
            };

            var x = customers.GroupJoin(
                accounts,
                c => c.CustomerId,
                a => a.CustomerId,
                (c, a) => new { c, a }
                ).SelectMany(
                    x => x.a.DefaultIfEmpty(), (c, a) => new { c, a })
                .Select(
                    x => new CustomerAccount()
                    {
                        CustomerId = x.c.c.CustomerId,
                        AccountId = x?.a?.AccountId ?? "",
                        Balance = x?.a?.Balance ?? "0",
                        Location = x.c.c.Location,
                        Username = x.c.c.Username
                    }
                );

            foreach (var i in x)
            {
                Console.WriteLine(i.ToString());
            }
        }
    }
}
