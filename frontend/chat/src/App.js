import { useState, useEffect } from "react";
import { WaitingRoom } from "./components/WaitingRoom.jsx";
import { HubConnectionBuilder } from "@microsoft/signalr";
import { Chat } from "./components/Chat.jsx";
import { useColorMode, useToast } from "@chakra-ui/react";
import { encryptMessage, decryptMessage } from "./utils/encryption";

/**
 * Главный компонент приложения React.
 * Управляет состоянием подключения к SignalR хабу, отображает комнату ожидания или интерфейс чата,
 * а также обрабатывает получение и отправку сообщений и системных уведомлений.
 */
const App = () => {
	// Состояния, управляющие UI и данными в приложении
	const [connection, setConnection] = useState(null); // Объект подключения к SignalR хабу
	const [messages, setMessages] = useState([]); // Список сообщений в текущем чате
	const [chatRoom, setChatRoom] = useState(""); // Название текущей комнаты чата
	const [currentUser, setCurrentUser] = useState(""); // Имя текущего пользователя
	const [users, setUsers] = useState([]); // Список пользователей в текущем чате
	const [error, setError] = useState(""); // Сообщение об ошибке для отображения пользователю
	const [isConnecting, setIsConnecting] = useState(false); // Флаг, показывающий, идет ли процесс подключения
	const [encryptionKey, setEncryptionKey] = useState(null); // Ключ шифрования для текущего чата
	const [encryptionIV, setEncryptionIV] = useState(null); // Вектор инициализации для текущего чата

	// Хуки Chakra UI для управления цветовым режимом и отображения уведомлений (тостов)
	const { colorMode } = useColorMode();
	const toast = useToast();

	// Эффект для очистки (остановки) SignalR подключения при размонтировании компонента App
	useEffect(() => {
		// Возвращаем функцию очистки
		return () => {
			// Если подключение существует, останавливаем его
			if (connection) {
				connection.stop();
			}
		};
	}, [connection]); // Зависимость от объекта подключения

	/**
	 * Асинхронная функция для установки соединения с SignalR хабом и присоединения к чату.
	 * Создает новое подключение, настраивает обработчики событий и отправляет запрос на присоединение к чату на сервер.
	 * @param {string} userName - Имя пользователя для присоединения.
	 * @param {string} chatRoom - Название комнаты чата для присоединения.
	 */
	const joinChat = async (userName, chatRoom) => {
		// Устанавливаем флаг подключения в true и сбрасываем предыдущие ошибки
		setIsConnecting(true);
		setError("");

		// Создаем новый экземпляр SignalR HubConnectionBuilder
		const backendUrl = process.env.REACT_APP_BACKEND_URL || "http://localhost:5022";
		const connection = new HubConnectionBuilder()
			//.withUrl("http://localhost:5022/chatHub") // Указываем URL SignalR хаба
			.withUrl(`${backendUrl}/chatHub`) // Указываем URL SignalR хаба
			.withAutomaticReconnect() // Настраиваем автоматическое переподключение
			.build(); // Собираем объект подключения

		// Флаг для отслеживания, произошла ли ошибка при вызове JoinChat на сервере
		let joinError = false;

		// Настраиваем обработчики событий, приходящих от сервера
		connection.on("ReceiveMessage", (userName, message, timestamp) => {
			// Обработчик получения нового сообщения.
			// Бэкенд теперь отправляет уже расшифрованный текст, просто добавляем его в список сообщений.
			setMessages((messages) => [...messages, { userName, message, timestamp }]);
		});

		connection.on("UpdateUserList", (users) => {
			// Обработчик обновления списка пользователей в чате.
			// Обновляем состояние списка пользователей.
			setUsers(users);
		});

		connection.on("ShowError", (message) => {
			// Обработчик получения сообщения об ошибке.
			// Устанавливаем сообщение об ошибке в состояние и отображаем тост.
			setError(message);
			joinError = true; // Устанавливаем флаг ошибки присоединения
			toast({
				title: "Ошибка",
				description: message,
				status: "error",
				duration: 4000,
				isClosable: true,
				position: "top"
			});
			// Останавливаем соединение при получении ошибки JoinChat
			connection.stop();
		});

		connection.on("SetEncryptionKeys", (key, iv) => {
			// Обработчик получения ключей шифрования от сервера.
			// Сохраняем ключи в состояние. (На данный момент используются только для загрузки истории).
			setEncryptionKey(key);
			setEncryptionIV(iv);
		});

		// Обработчик системных уведомлений (не ошибок)
		connection.on("ReceiveSystemNotification", (message) => {
			// Обработчик получения системного уведомления.
			// Отображаем информационный тост.
			toast({
				title: "Уведомление",
				description: message,
				status: "info", // Используем статус info или success для уведомлений
				duration: 4000,
				isClosable: true,
				position: "top"
			});
		});

		// Обработка события отключения от хаба
		connection.onclose((error) => {
			// Если отключение произошло из-за ошибки, выводим сообщение
			if (error) {
				setError("Соединение было разорвано");
				toast({
					title: "Ошибка подключения",
					description: "Соединение с сервером было разорвано",
					status: "error",
					duration: 4000,
					isClosable: true,
					position: "top"
				});
			}
			// Сбрасываем все состояния, связанные с подключением и чатом
			setConnection(null);
			setCurrentUser("");
			setMessages([]);
			setUsers([]);
			setEncryptionKey(null);
			setEncryptionIV(null);
		});

		try {
			// Запускаем SignalR соединение
			await connection.start();
			// Отправляем на сервер запрос на присоединение к чату с именем пользователя и названием чата
			await connection.invoke("JoinChat", { userName, chatRoom });

			// Если при вызове JoinChat на сервере произошла ошибка (флаг joinError стал true)
			if (joinError) {
				// Не сохраняем объект подключения в состоянии, чтобы остаться в WaitingRoom
				return;
			}

			// Если подключение и присоединение прошли успешно
			setConnection(connection); // Сохраняем объект подключения
			setChatRoom(chatRoom); // Сохраняем название чата
			setCurrentUser(userName); // Сохраняем имя пользователя
		} catch (error) {
			// Обработка ошибок, которые могут возникнуть при старте соединения или вызове invoke
			console.error("Ошибка подключения:", error);
			setError("Не удалось подключиться к серверу");
			toast({
				title: "Ошибка подключения",
				description: "Не удалось подключиться к серверу. Попробуйте позже.",
				status: "error",
				duration: 4000,
				isClosable: true,
				position: "top"
			});
		} finally {
			// Сбрасываем флаг подключения независимо от результата
			setIsConnecting(false);
		}
	};

	/**
	 * Асинхронная функция для отправки сообщения в текущий чат.
	 * Отправляет текст сообщения на сервер через SignalR хаб.
	 * @param {string} message - Текст сообщения для отправки.
	 */
	const sendMessage = async (message) => {
		try {
			// Проверяем, что соединение активно
			if (connection) {
				// Отправляем незашифрованный текст сообщения на бэкенд методом SendMessage хаба
				await connection.invoke("SendMessage", message);
			}
		} catch (error) {
			// Обработка ошибок при отправке сообщения
			console.error("Ошибка отправки сообщения:", error);
			setError("Не удалось отправить сообщение");
			toast({
				title: "Ошибка",
				description: "Не удалось отправить сообщение. Попробуйте еще раз.",
				status: "error",
				duration: 3000,
				isClosable: true,
				position: "top"
			});
		}
	};

	/**
	 * Асинхронная функция для закрытия текущего SignalR подключения.
	 * Останавливает соединение и сбрасывает все связанные состояния.
	 */
	const closeChat = async () => {
		try {
			// Если соединение существует, останавливаем его
			if (connection) {
				await connection.stop();
			}
		} catch (error) {
			// Обработка ошибок при остановке соединения
			console.error("Ошибка при закрытии чата:", error);
		} finally {
			// Сбрасываем все состояния, связанные с подключением и чатом,
			// чтобы вернуться в WaitingRoom.
			setConnection(null);
			setCurrentUser("");
			setMessages([]);
			setUsers([]);
			setEncryptionKey(null);
			setEncryptionIV(null);
		}
	};

	// Рендеринг компонента в зависимости от состояния подключения
	return (
		<div className={`min-h-screen flex items-center justify-center transition-colors duration-200 ${
			colorMode === 'dark' ? 'bg-gray-900' : 'bg-gray-100'
		}`}>
			{connection ? ( // Если соединение установлено (пользователь в чате)
				<Chat
					messages={messages} // Передаем список сообщений
					sendMessage={sendMessage} // Передаем функцию отправки сообщения
					closeChat={closeChat} // Передаем функцию закрытия чата
					chatRoom={chatRoom} // Передаем название чата
					currentUser={currentUser} // Передаем имя текущего пользователя
					users={users} // Передаем список пользователей
					error={error} // Передаем текущую ошибку (если есть)
					connection={connection} // Передаем объект подключения (можно убрать, если не нужен в Chat)
				/>
			) : ( // Если соединение не установлено (пользователь в комнате ожидания)
				<WaitingRoom joinChat={joinChat} /> // Передаем функцию для присоединения к чату
			)}
		</div>
	);
};

export default App; // Экспортируем компонент App по умолчанию
