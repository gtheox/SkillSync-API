// This file makes the Program class accessible to tests
// In .NET 8, top-level statements generate an implicit Program class
// This partial class declaration makes it accessible for WebApplicationFactory
namespace SkillSync.API;

public partial class Program
{
    // This partial class makes Program accessible to tests
}

