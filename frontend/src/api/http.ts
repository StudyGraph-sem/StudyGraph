export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
  ) {
    super(message)
  }
}

export async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers)
  headers.set('X-StudyGraph-CSRF', '1')
  if (options.body) headers.set('Content-Type', 'application/json')
  let response: Response
  try {
    response = await fetch(`/api${path}`, {
      ...options,
      headers,
      credentials: 'include',
      cache: 'no-store',
    })
  } catch {
    throw new ApiError(
      0,
      'Не удалось связаться с сервером. Проверьте подключение и повторите попытку.',
    )
  }
  if (!response.ok) {
    const messages: Record<number, string> = {
      400: 'Проверьте введённые данные.',
      401: 'Неверный email или пароль, либо сессия завершена.',
      403: 'Доступ запрещён. Проверьте адрес сайта и повторите попытку.',
      404: 'Пользователь не найден.',
      409: 'Пользователь с таким email уже зарегистрирован.',
      429: 'Слишком много попыток. Подождите минуту и попробуйте снова.',
    }
    throw new ApiError(
      response.status,
      messages[response.status] ?? 'Ошибка сервера. Попробуйте позже.',
    )
  }
  return response.status === 204 ? (undefined as T) : ((await response.json()) as T)
}
