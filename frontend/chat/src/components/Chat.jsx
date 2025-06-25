import { Button, CloseButton, Heading, Input, useColorMode, Tooltip, useToast } from "@chakra-ui/react";
import { useEffect, useRef, useState } from "react";
import { Message } from "./Message";

/**
 * Компонент чата
 * @param {Object} props - Свойства компонента
 * @param {Array} props.messages - Массив сообщений
 * @param {string} props.chatRoom - Название чата
 * @param {Function} props.sendMessage - Функция отправки сообщения
 * @param {Function} props.closeChat - Функция закрытия чата
 * @param {string} props.currentUser - Текущий пользователь
 * @param {Array} props.users - Массив пользователей
 * @param {string} props.error - Сообщение об ошибке
 * @param {object} props.connection - Объект SignalR соединения
 */
export const Chat = ({ messages, chatRoom, sendMessage, closeChat, currentUser, users = [], error, connection }) => {
	// Состояние для текста сообщения
	const [message, setMessage] = useState("");
	// Реф для автоматической прокрутки к последнему сообщению
	const messagesEndRef = useRef(null);
	const { colorMode, toggleColorMode } = useColorMode();
	const toast = useToast();

	// Автоматическая прокрутка к последнему сообщению
	useEffect(() => {
		messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
	}, [messages]);

	// Отображение ошибки, если она есть
	useEffect(() => {
		if (error) {
			toast({
				title: "Ошибка",
				description: error,
				status: "error",
				duration: 4000,
				isClosable: true,
				position: "top"
			});
		}
	}, [error, toast]);

	/**
	 * Валидация сообщения
	 * @param {string} text - Текст сообщения
	 * @returns {string|null} Сообщение об ошибке или null
	 */
	const validateMessage = (text) => {
		if (!text.trim()) {
			return "Сообщение не может быть пустым";
		}
		if (text.length > 1000) {
			return "Сообщение слишком длинное (максимум 1000 символов)";
		}
		return null;
	};

	/**
	 * Обработка отправки сообщения
	 */
	const onSendMessage = () => {
		const error = validateMessage(message);
		if (error) {
			toast({
				title: "Ошибка",
				description: error,
				status: "error",
				duration: 3000,
				isClosable: true,
				position: "top"
			});
			return;
		}

		sendMessage(message);
		setMessage("");
	};

	/**
	 * Обработка нажатия клавиш
	 * @param {KeyboardEvent} e - Событие клавиатуры
	 */
	const handleKeyPress = (e) => {
		if (e.key === 'Enter' && !e.shiftKey) {
			e.preventDefault();
			onSendMessage();
		}
	};

	return (
		<div className={`w-full max-w-lg sm:p-8 p-3 rounded shadow-lg transition-colors duration-200 mx-2 ${
			colorMode === 'dark' 
				? 'bg-background-dark text-text-dark' 
				: 'bg-background-light text-text-light'
		}`}>
			{/* Заголовок чата */}
			<div className="flex flex-row justify-between mb-5 items-center">
				<Heading size="lg" className="truncate">{chatRoom}</Heading>
				<div className="flex gap-2 items-center">
					{/* Счётчик онлайн */}
					<Tooltip label={users.length > 0 ? users.join(", ") : "Нет пользователей"} placement="bottom" hasArrow>
						<div className="flex items-center select-none cursor-pointer px-2 py-1 rounded bg-gray-700 text-white text-sm font-semibold">
							<svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" strokeWidth={1.5} stroke="currentColor" className="w-5 h-5 mr-1">
								<path strokeLinecap="round" strokeLinejoin="round" d="M16.5 7.5a3 3 0 11-6 0 3 3 0 016 0zM19.5 19.5v-1.125A2.625 2.625 0 0016.875 15.75h-9.75A2.625 2.625 0 004.5 18.375V19.5" />
							</svg>
							{users.length}
						</div>
					</Tooltip>
					{/* Кнопка удаления чата */}
					<Button colorScheme="red" size="sm" onClick={() => {
						if (window.confirm('Вы уверены, что хотите удалить этот чат и всю его историю?')) {
							connection.invoke("DeleteChat", chatRoom)
								.then(() => {
									// После успешного удаления на backend, закрываем окно чата
									closeChat();
								})
								.catch(err => {
									console.error("Ошибка при вызове DeleteChat:", err);
									toast({
										title: "Ошибка",
										description: "Не удалось удалить чат.",
										status: "error",
										duration: 4000,
										isClosable: true,
										position: "top"
									});
								});
						}
					}}>
						Удалить чат
					</Button>
					<Button onClick={toggleColorMode} size="sm" className="min-w-[2.5rem]">{colorMode === 'dark' ? '🌞' : '🌙'}</Button>
					<CloseButton onClick={closeChat} />
				</div>
			</div>

			{/* Область сообщений */}
			<div className="flex flex-col overflow-auto scroll-smooth gap-3 pb-3 pr-3 custom-scrollbar scrollbar-thin scrollbar-thumb-gray-400/70 scrollbar-thumb-rounded-full scrollbar-track-transparent scrollbar-fade" style={{height: '60vh', minHeight: 200}}>
				{messages.length === 0 ? (
					<div className="text-center text-gray-500 mt-4">
						Нет сообщений. Будьте первым, кто напишет!
					</div>
				) : (
					messages.map((messageInfo, index) => (
						<Message 
							messageInfo={messageInfo} 
							key={index} 
							currentUser={currentUser}
							colorMode={colorMode}
						/>
					))
				)}
				<span ref={messagesEndRef} />
			</div>

			{/* Форма отправки сообщения */}
			<div className="flex gap-2 mt-2 flex-col sm:flex-row">
				<Input
					type="text"
					value={message}
					onChange={(e) => setMessage(e.target.value)}
					onKeyPress={handleKeyPress}
					placeholder="Введите сообщение"
					className={colorMode === 'dark' ? 'bg-gray-800' : ''}
					size="md"
					maxLength={1000}
				/>
				<Button 
					colorScheme="blue" 
					onClick={onSendMessage} 
					size="md" 
					minWidth="100px"
					isDisabled={!message.trim()}
				>
					Отправить
				</Button>
			</div>
		</div>
	);
};
