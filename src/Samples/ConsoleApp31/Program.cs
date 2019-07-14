using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSaga;
using NSaga.Autofac;
using NSaga.EF;
using Samples;
using System;

namespace ConsoleApp31
{
    public class InternalContainerSample
    {
        private ISagaMediator sagaMediator;
        private ISagaRepository sagaRepository;

        public void Run()
        {
            var builder = new ContainerBuilder();

            var options = new DbContextOptionsBuilder<MyContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    // don't raise the error warning us that the in memory db doesn't support transactions
                    .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                    .Options;

            //var options = new DbContextOptionsBuilder<MyContext>()
            //            .UseNpgsql("Host=localhost;Port=5432;Database=demodb;Username=postgres;Password=postgres")
            //            .Options;

            builder.RegisterType<MyContext>()
              .WithParameter("options", options)
              .InstancePerLifetimeScope();

            builder
                .RegisterNSagaComponents()
                .UseSqlServer()
                .WithDatabase<SagaSqlDatabase<MyContext>>();

            var container = builder.Build();

            sagaMediator = container.Resolve<ISagaMediator>();
            sagaRepository = container.Resolve<ISagaRepository>();

            var correlationId = Guid.NewGuid();

            StartSaga(correlationId);

            RequestVerificationCode(correlationId);

            ProvideVerificationCode(correlationId);

            CreateAccount(correlationId);

            var saga = sagaRepository.Find<AccountCreationSaga>(correlationId);
            var jamesName = saga.SagaData.Person.FullName;
            Console.WriteLine($"Taking information from SagaData; Person.FullName='{jamesName}'");
        }


        private void StartSaga(Guid correlationId)
        {
            var initialMessage = new PersonalDetailsVerification(correlationId)
            {
                DateOfBirth = new DateTime(1920, 11, 11),
                FirstName = "James",
                LastName = "Bond",
                HomePostcode = "MI6 HQ",
                PayrollNumber = "007",
            };

            var result = sagaMediator.Consume(initialMessage);
            if (!result.IsSuccessful)
            {
                Console.WriteLine(result.ToString());
            }
        }


        private void RequestVerificationCode(Guid correlationId)
        {
            var verificationRequest = new VerificationCodeRequest(correlationId);

            sagaMediator.Consume(verificationRequest);
        }

        private void ProvideVerificationCode(Guid correlationId)
        {
            var verificationCode = new VerificationCodeProvided(correlationId)
            {
                VerificationCode = "123456",
            };
            sagaMediator.Consume(verificationCode);
        }


        private void CreateAccount(Guid correlationId)
        {
            var accountCreation = new AccountDetailsProvided(correlationId)
            {
                Username = "James.Bond",
                Password = "James Is Awesome!",
                PasswordConfirmation = "james is awesome",
            };

            var excutionResult = sagaMediator.Consume(accountCreation);

            if (!excutionResult.IsSuccessful)
            {
                Console.WriteLine(excutionResult.ToString());
            }
        }
    }

    // Reserve car          => book hotel           => book flight
    //     \ Cancel car         \ cancel hotel          \ cancel flight


    /*
     SagaBuilder saga = SagaBuilder.newSaga("trip")
        .activity("Reserve car", ReserveCarAdapter.class) 
        .compensationActivity("Cancel car", CancelCarAdapter.class) 
        .activity("Book hotel", BookHotelAdapter.class) 
        .compensationActivity("Cancel hotel", CancelHotelAdapter.class) 
        .activity("Book flight", BookFlightAdapter.class) 
        .compensationActivity("Cancel flight", CancelFlightAdapter.class) 
        .end()
        .triggerCompensationOnAnyError();
    */

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            new InternalContainerSample().Run();
        }
    }
}
