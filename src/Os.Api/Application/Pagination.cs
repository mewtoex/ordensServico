using Os.Api.Domain;

namespace Os.Api.Application;

public static class Pagination
{
    public static void Validate(int page, int pageSize)
    {
        if (page < 1 || page > 1_000_000 || pageSize < 1 || pageSize > 100)
        {
            throw new BusinessException("Página deve ser positiva e tamanho entre 1 e 100.");
        }
    }
}
