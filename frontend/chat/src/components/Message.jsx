/**
 * Компонент сообщения
 * @param {Object} props - Свойства компонента
 * @param {Object} props.messageInfo - Информация о сообщении
 * @param {string} props.messageInfo.userName - Имя отправителя
 * @param {string} props.messageInfo.message - Текст сообщения
 * @param {string} props.messageInfo.timestamp - Время отправки
 * @param {string} props.currentUser - Текущий пользователь
 * @param {string} props.colorMode - Текущая тема оформления
 */
export const Message = ({ messageInfo, currentUser, colorMode }) => {
	// Определяем, является ли сообщение собственным
	const isOwnMessage = messageInfo.userName === currentUser;

	return (
		<div className={`flex ${isOwnMessage ? 'justify-end' : 'justify-start'} w-full`}>
			<div className={`flex flex-col max-w-[70%] ${isOwnMessage ? 'items-end' : 'items-start'}`}>
				{/* Имя отправителя */}
				<span className={`text-sm mb-1 ${
					colorMode === 'dark' ? 'text-gray-400' : 'text-slate-600'
				}`}>
					{messageInfo.userName}
				</span>

				{/* Текст сообщения */}
				<div className={`p-2 rounded-lg shadow-md ${
					isOwnMessage 
						? 'bg-primary-light text-white' 
						: colorMode === 'dark'
							? 'bg-gray-700 text-gray-100'
							: 'bg-gray-100'
				}`} style={{ wordBreak: 'break-word' }}>
					{messageInfo.message}
				</div>

				{/* Время отправки */}
				<span className={`text-xs mt-1 ${
					colorMode === 'dark' ? 'text-gray-500' : 'text-slate-500'
				}`}>
					{messageInfo.timestamp}
				</span>
			</div>
		</div>
	);
};
