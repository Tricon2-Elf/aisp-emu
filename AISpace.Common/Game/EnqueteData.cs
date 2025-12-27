namespace AISpace.Common.Game;

public class EnqueteData(uint id, string question, List<string> answers)
{

    public byte[] ToBytes()
    {
        while(answers.Count < 10)
            answers.Add("");
        var writer = new Network.PacketWriter();
        writer.Write(id);
        writer.WriteFixedJisString(question, 181);
        foreach(var answer in answers)
            writer.WriteFixedJisString(answer, 61);
        return writer.ToBytes();
    }
}
