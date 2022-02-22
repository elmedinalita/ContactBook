namespace ContactBook.Models
{
    public class ManagableContact
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
    }
    public class Contact
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public List<Email> Email { get; set; } = new List<Email>();
        public List<Phone> Phone { get; set; } = new List<Phone>();
        public List<Address> Address { get; set; } = new List<Address>();
    }

    public class Phone
    {
        public int Id { get; set; }
        public string PhoneNo { get; set; } = "";
    }
    public class Email
    {
        public int Id { get; set; }
        public string EmailAddr { get; set; } = "";
    }
    public class Address
    {
        public int Id { get; set; }
        public string Addr { get; set; } = "";
    }
}
