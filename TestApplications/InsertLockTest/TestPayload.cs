namespace InsertLockTest
{
    internal class TestPayload
    {
        public string FirstName { get; set; } = "John";
        public string LastName { get; set; } = "Doe";
        public DateTime BirthDate { get; set; } = DateTime.Now.AddYears(-30);
    }
}
