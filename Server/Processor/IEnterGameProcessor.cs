namespace ServerSkills.Processor;

public interface IEnterGameProcessor
{
    void Process(ClientSession clientSession, int requestId);
}