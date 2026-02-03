// Custom hook for API data fetching with loading and error states
import { useState, useEffect, useCallback } from 'react';

interface UseApiOptions<T> {
  initialData?: T;
  onSuccess?: (data: T) => void;
  onError?: (error: Error) => void;
  immediate?: boolean;
}

export const useApi = <T,>(
  apiFunc: () => Promise<{ success: boolean; data?: T; error?: string }>,
  options: UseApiOptions<T> = {}
) => {
  const { initialData, onSuccess, onError, immediate = true } = options;
  
  const [data, setData] = useState<T | undefined>(initialData);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const execute = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);
      
      const response = await apiFunc();
      
      if (response.success && response.data !== undefined) {
        setData(response.data);
        onSuccess?.(response.data);
      } else {
        const errorMsg = response.error || 'Request failed';
        setError(errorMsg);
        onError?.(new Error(errorMsg));
      }
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'An error occurred';
      setError(errorMsg);
      onError?.(err instanceof Error ? err : new Error(errorMsg));
    } finally {
      setIsLoading(false);
    }
  }, [apiFunc, onSuccess, onError]);

  useEffect(() => {
    if (immediate) {
      execute();
    }
  }, [immediate]);

  const refetch = useCallback(() => {
    return execute();
  }, [execute]);

  return {
    data,
    isLoading,
    error,
    refetch,
    execute,
  };
};

// Hook for mutations (POST, PUT, DELETE)
export const useMutation = <TData, TVariables = void>(
  apiFunc: (variables: TVariables) => Promise<{ success: boolean; data?: TData; error?: string }>,
  options: UseApiOptions<TData> = {}
) => {
  const { onSuccess, onError } = options;
  
  const [data, setData] = useState<TData | undefined>();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const mutate = useCallback(
    async (variables: TVariables) => {
      try {
        setIsLoading(true);
        setError(null);
        
        const response = await apiFunc(variables);
        
        if (response.success && response.data !== undefined) {
          setData(response.data);
          onSuccess?.(response.data);
          return response.data;
        } else {
          const errorMsg = response.error || 'Request failed';
          setError(errorMsg);
          onError?.(new Error(errorMsg));
          throw new Error(errorMsg);
        }
      } catch (err) {
        const errorMsg = err instanceof Error ? err.message : 'An error occurred';
        setError(errorMsg);
        onError?.(err instanceof Error ? err : new Error(errorMsg));
        throw err;
      } finally {
        setIsLoading(false);
      }
    },
    [apiFunc, onSuccess, onError]
  );

  const reset = useCallback(() => {
    setData(undefined);
    setError(null);
  }, []);

  return {
    data,
    isLoading,
    error,
    mutate,
    reset,
  };
};
