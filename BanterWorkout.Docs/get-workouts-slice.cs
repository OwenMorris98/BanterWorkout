// Features/Workouts/GetWorkouts/GetWorkoutsEndpoint.cs
namespace Features.Workouts.GetWorkouts;

public class GetWorkoutsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/api/workouts", async (
            [AsParameters] GetWorkoutsQuery query,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(query, ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithName("GetWorkouts")
        .WithOpenApi();
    }
}

// Features/Workouts/GetWorkouts/GetWorkoutsQuery.cs
public record GetWorkoutsQuery : IRequest<PaginatedList<WorkoutResponse>>
{
    public string? SearchTerm { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

// Features/Workouts/GetWorkouts/WorkoutResponse.cs
public record WorkoutResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public DateTime Date { get; init; }
    public List<ExerciseResponse> Exercises { get; init; }
    public bool IsShared { get; init; }
}

public record ExerciseResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public int Sets { get; init; }
    public int Reps { get; init; }
    public decimal Weight { get; init; }
}

// Features/Workouts/GetWorkouts/GetWorkoutsValidator.cs
public class GetWorkoutsValidator : AbstractValidator<GetWorkoutsQuery>
{
    public GetWorkoutsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0);

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(50);

        RuleFor(x => x.ToDate)
            .GreaterThanOrEqualTo(x => x.FromDate)
            .When(x => x.FromDate.HasValue && x.ToDate.HasValue);
    }
}

// Features/Workouts/GetWorkouts/GetWorkoutsHandler.cs
public class GetWorkoutsHandler : IRequestHandler<GetWorkoutsQuery, PaginatedList<WorkoutResponse>>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetWorkoutsHandler(
        AppDbContext context,
        ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<PaginatedList<WorkoutResponse>> Handle(
        GetWorkoutsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Workouts
            .Include(w => w.Exercises)
            .Where(w => w.UserId == _currentUserService.UserId)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(w => 
                w.Name.Contains(request.SearchTerm) ||
                w.Exercises.Any(e => e.Name.Contains(request.SearchTerm)));
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(w => w.Date >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(w => w.Date <= request.ToDate.Value);
        }

        // Order by most recent
        query = query.OrderByDescending(w => w.Date);

        var totalItems = await query.CountAsync(cancellationToken);
        
        var workouts = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var workoutResponses = _mapper.Map<List<WorkoutResponse>>(workouts);

        return new PaginatedList<WorkoutResponse>(
            workoutResponses,
            totalItems,
            request.Page,
            request.PageSize);
    }
}

// Features/Workouts/GetWorkouts/WorkoutMappingProfile.cs
public class WorkoutMappingProfile : Profile
{
    public WorkoutMappingProfile()
    {
        CreateMap<Workout, WorkoutResponse>();
        CreateMap<Exercise, ExerciseResponse>();
    }
}

// Shared/PaginatedList.cs
public class PaginatedList<T>
{
    public List<T> Items { get; }
    public int PageNumber { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }

    public PaginatedList(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        TotalCount = totalCount;
        Items = items;
    }

    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
