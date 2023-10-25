// Copyright (c) Microsoft. All rights reserved.

namespace Microsoft.SemanticKernel.AI.ChatCompletion;

/// <summary>
/// Chat message abstraction
/// </summary>
public abstract class ChatMessageBase
{
    /// <summary>
    /// Role of the author of the message
    /// </summary>
    public AuthorRole Role { get; set; }

    /// <summary>
    /// Content of the message
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// The name of the author of this message. `name` is required if role is `function`, and it should be the name of the
    /// function whose response is in the `content`. May contain a-z, A-Z, 0-9, and underscores, with a maximum length of
    /// 64 characters.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Creates a new instance of the <see cref="ChatMessageBase"/> class
    /// </summary>
    /// <param name="role">Role of the author of the message</param>
    /// <param name="content">Content of the message</param>
    protected ChatMessageBase(AuthorRole role, string content)
    {
        this.Role = role;
        this.Content = content;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="ChatMessageBase"/> class
    /// </summary>
    /// <param name="role">Role of the author of the message</param>
    /// <param name="content">Content of the message</param>
    /// <param name="name">name of the message author (required if role is `function`</param>
    protected ChatMessageBase(AuthorRole role, string content, string name)
    {
        this.Role = role;
        this.Content = content;
        this.Name = name;
    }
}
