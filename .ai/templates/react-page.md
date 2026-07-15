# Template: React Page

## Metadata
- **Name**: react-page
- **Purpose**: React page component for FuelTrack frontend
- **Layer**: Frontend
- **Path**: `frontend/src/pages/{Name}.jsx`

## Template

```jsx
import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'react-toastify';
import client from '../api/client';
import { Button, Table, Spinner, Alert, Modal, Form } from 'react-bootstrap';

const {Name} = () => {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [showModal, setShowModal] = useState(false);
  const [formData, setFormData] = useState({ name: '' });

  const { data, isLoading, error } = useQuery({
    queryKey: ['{entities}', page],
    queryFn: async () => {
      const response = await client.get('/api/{entities}', { params: { page, pageSize: 20 } });
      return response.data;
    },
  });

  const createMutation = useMutation({
    mutationFn: async (newData) => {
      return client.post('/api/{entities}', newData);
    },
    onSuccess: () => {
      queryClient.invalidateQueries(['{entities}']);
      toast.success(t('{entity}.created'));
      setShowModal(false);
      setFormData({ name: '' });
    },
    onError: () => {
      toast.error(t('{entity}.createError'));
    },
  });

  const handleSubmit = (e) => {
    e.preventDefault();
    createMutation.mutate(formData);
  };

  if (isLoading) return <Spinner animation="border" />;
  if (error) return <Alert variant="danger">{t('common.error')}</Alert>;

  return (
    <div className="container-fluid">
      <div className="d-flex justify-content-between align-items-center mb-3">
        <h2>{t('{entity}.title')}</h2>
        <Button onClick={() => setShowModal(true)}>{t('common.create')}</Button>
      </div>

      <Table striped bordered hover>
        <thead>
          <tr>
            <th>{t('common.id')}</th>
            <th>{t('{entity}.name')}</th>
            <th>{t('common.actions')}</th>
          </tr>
        </thead>
        <tbody>
          {data?.data?.map((item) => (
            <tr key={item.id}>
              <td>{item.id}</td>
              <td>{item.name}</td>
              <td>
                <Button size="sm">{t('common.edit')}</Button>
              </td>
            </tr>
          ))}
        </tbody>
      </Table>

      <Modal show={showModal} onHide={() => setShowModal(false)}>
        <Modal.Header closeButton>
          <Modal.Title>{t('{entity}.createTitle')}</Modal.Title>
        </Modal.Header>
        <Form onSubmit={handleSubmit}>
          <Modal.Body>
            <Form.Group>
              <Form.Label>{t('{entity}.name')}</Form.Label>
              <Form.Control
                type="text"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                required
              />
            </Form.Group>
          </Modal.Body>
          <Modal.Footer>
            <Button variant="secondary" onClick={() => setShowModal(false)}>
              {t('common.cancel')}
            </Button>
            <Button type="submit" disabled={createMutation.isPending}>
              {createMutation.isPending ? <Spinner size="sm" /> : t('common.save')}
            </Button>
          </Modal.Footer>
        </Form>
      </Modal>
    </div>
  );
};

export default {Name};
```

## Checklist
- [ ] Uses `useTranslation()` hook
- [ ] Uses `client` from `../api/client` (not raw axios)
- [ ] Uses React Query for data fetching
- [ ] Loading state with `Spinner`
- [ ] Error state with `Alert`
- [ ] Empty state handled
- [ ] Form validation
- [ ] Toast notifications for success/error
- [ ] All strings use `t('key')`
- [ ] Responsive layout
