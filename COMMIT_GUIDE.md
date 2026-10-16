# Suggested commit sequence (aim for 20+ real commits)

Make each commit when that step works. Do not squash them. Delete this file before submitting if you like.

1. Create solution and API project (`dotnet new`)
2. Add NuGet packages and `.gitignore`
3. Add enums and `User`/`Organiser`/`Participant` entities
4. Add `Event`, `EventRoute`, `EventCategory` entities
5. Add `Registration` and `Result` entities
6. Add `RaceDayDbContext` and relationships
7. Add connection string and initial migration
8. Configure cookie authentication and Program.cs
9. Add auth DTOs and `UserFactory` (password hashing)
10. Add `AuthController` (register, login, logout, me)
11. Add event DTOs and `EventsController`
12. Add `RoutesController`
13. Add `EventCategoriesController`
14. Add participant and organiser profile controllers
15. Add `RegistrationsController`
16. Add `ResultsController`
17. Add Swagger XML comments and configuration
18. Add test project and `RaceDayFactory`
19. Add authentication tests
20. Add event management and role tests
21. Add registration (enrolment) tests
22. Add result tests
23. Add GitHub Actions workflow
24. Add README, green build screenshot, video link
