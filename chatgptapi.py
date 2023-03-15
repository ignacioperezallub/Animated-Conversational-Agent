import openai

openai.api_key = ""

chat_history = []

def generate_response(prompt):
    response = openai.ChatCompletion.create(
        model='gpt-3.5-turbo',
        messages=chat_history,
        temperature=0.2,
        max_tokens=250
    )
    message = response.choices[0]["message"]['content']
    return message

while True:
    user_input = input("You: ")
    chat_history.append({"role": "user", "content": user_input})
    # if user_input.lower() == "bye":
    #     print("ChatGPT: Goodbye!")
    #     break
    prompt = f"You: {user_input}\nChatGPT:"
    response = generate_response(chat_history)
    chat_history.append({"role": "assistant", "content": response})
    print(f"ChatGPT: {response}")

