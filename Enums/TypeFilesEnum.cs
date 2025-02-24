namespace DogTracker.Enums;

public enum TypeFilesEnum
{
    Ordonnances,
    CarnetDeSante,
    Divers
}

public static class TypeFilesEnumExtensions
{
    public static Dictionary<TypeFilesEnum, string> GetCategories()
    {
        return new Dictionary<TypeFilesEnum, string>
        {
            { TypeFilesEnum.Ordonnances, TypeFilesEnum.Ordonnances.GetCategory() },
            { TypeFilesEnum.CarnetDeSante, TypeFilesEnum.CarnetDeSante.GetCategory() },
            { TypeFilesEnum.Divers, TypeFilesEnum.Divers.GetCategory() }
        };
    }
    public static string GetCategory(this TypeFilesEnum typeFilesEnum)
    {
        return typeFilesEnum switch
        {
            TypeFilesEnum.Ordonnances => "Ordonnances",
            TypeFilesEnum.CarnetDeSante => "Carnet de santé",
            TypeFilesEnum.Divers => "Divers",
            _ => throw new ArgumentOutOfRangeException(nameof(typeFilesEnum), typeFilesEnum, null)
        };
    }
}