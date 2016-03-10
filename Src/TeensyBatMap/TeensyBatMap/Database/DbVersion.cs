namespace TeensyBatMap.Database
{
    [Table("DbVersion")]
    public class DbVersion
    {
        [PrimaryKey]
        [NotNull]
        public int Id { get; set; }

        [NotNull]
        public int Version { get; set; }

        [NotNull]
        public string Name { get; set; }
    }
}