// Mirrors the problem-details shape written by both services' ExceptionHandlingMiddleware.
export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly title: string,
    public readonly errors: string[]
  ) {
    super(title);
    this.name = 'ApiError';
  }
}

export async function throwIfNotOk(response: Response): Promise<void> {
  if (response.ok) return;

  let title = `Request failed with status ${response.status}`;
  let errors: string[] = [];
  try {
    const body = await response.json();
    title = body.title ?? title;
    errors = body.errors ?? [];
  } catch {
    // Non-JSON error body (e.g. an empty 401/403) — fall back to the generic title above.
  }
  throw new ApiError(response.status, title, errors);
}
