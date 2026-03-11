namespace JLPTWordbook.Shared;

public record struct Word(WordComponent[] Components, string Ko, string En, Func<string?> Localizer);
