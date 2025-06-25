import { Button, Heading, Input, Text, useColorMode } from "@chakra-ui/react";
import { useState } from "react";

/**
 * Компонент комнаты ожидания
 * @param {Object} props - Свойства компонента
 * @param {Function} props.joinChat - Функция подключения к чату
 */
export const WaitingRoom = ({ joinChat }) => {
	// Состояния для формы
	const [userName, setUserName] = useState("");
	const [chatRoom, setChatRoom] = useState("");
	const [errors, setErrors] = useState({});
	const { colorMode, toggleColorMode } = useColorMode();

	/**
	 * Валидация полей формы
	 * @returns {boolean} Результат валидации
	 */
	const validateForm = () => {
		const newErrors = {};
		
		if (!userName.trim()) {
			newErrors.userName = "Имя пользователя обязательно";
		} else if (userName.length < 2) {
			newErrors.userName = "Имя должно содержать минимум 2 символа";
		}
		
		if (!chatRoom.trim()) {
			newErrors.chatRoom = "Название чата обязательно";
		} else if (chatRoom.length < 3) {
			newErrors.chatRoom = "Название чата должно содержать минимум 3 символа";
		}

		setErrors(newErrors);
		return Object.keys(newErrors).length === 0;
	};

	/**
	 * Обработка отправки формы
	 * @param {Event} e - Событие формы
	 */
	const onSubmit = (e) => {
		e.preventDefault();
		if (validateForm()) {
			// Нормализация названия чата
			const normalizedRoom = chatRoom.trim().toLowerCase();
			joinChat(userName.trim(), normalizedRoom);
		}
	};

	return (
		<form
			onSubmit={onSubmit}
			className={`w-full max-w-sm sm:p-8 p-4 rounded shadow-lg transition-colors duration-200 mx-2 ${
				colorMode === 'dark' ? 'bg-background-dark text-text-dark' : 'bg-white text-gray-900'
			}`}
		>
			{/* Заголовок */}
			<div className="flex flex-row justify-between items-center mb-4">
				<Heading size="lg" className="truncate">Онлайн чат</Heading>
				<Button onClick={toggleColorMode} size="sm" className="min-w-[2.5rem]">
					{colorMode === 'dark' ? '🌞' : '🌙'}
				</Button>
			</div>

			{/* Поле ввода имени */}
			<div className="mb-4">
				<Text fontSize={"sm"} className={colorMode === 'dark' ? 'text-gray-300' : 'text-gray-600'}>
					Имя пользователя
				</Text>
				<Input
					name="username"
					placeholder="Введите ваше имя"
					value={userName}
					onChange={(e) => setUserName(e.target.value)}
					className={`${colorMode === 'dark' ? 'bg-gray-800 text-gray-100 placeholder-gray-400' : ''} ${
						errors.userName ? 'border-red-500' : ''
					}`}
					size="md"
					py={6}
				/>
				{errors.userName && (
					<Text fontSize="sm" color="red.500" mt={1}>
						{errors.userName}
					</Text>
				)}
			</div>

			{/* Поле ввода названия чата и кнопка генерации */}
			<div className="mb-6">
				<Text fontSize={"sm"} className={colorMode === 'dark' ? 'text-gray-300' : 'text-gray-600'}>
					Название чата
				</Text>
				<div className="flex items-center gap-2">
					<Input
						name="chatname"
						placeholder="Введите название чата"
						value={chatRoom}
						onChange={(e) => setChatRoom(e.target.value)}
						className={`${colorMode === 'dark' ? 'bg-gray-800 text-gray-100 placeholder-gray-400' : ''} ${errors.chatRoom ? 'border-red-500' : ''}`}
						size="md"
						py={6}
					/>
					<Button onClick={() => {
						const characters = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()';
						let randomName = '';
						for (let i = 0; i < 16; i++) {
							const randomIndex = Math.floor(Math.random() * characters.length);
							randomName += characters[randomIndex];
						}
						setChatRoom(randomName);
					}} size="md" py={6}>
						🎲
					</Button>
				</div>
				{errors.chatRoom && (
					<Text fontSize="sm" color="red.500" mt={1}>
						{errors.chatRoom}
					</Text>
				)}
			</div>

			{/* Кнопка подключения */}
			<Button
				type="submit"
				colorScheme="blue"
				width="full"
				size="lg"
			>
				Подключиться
			</Button>
		</form>
	);
};
